using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Exceptions;
using WorkBoard.Domain.Exceptions;

namespace WorkBoard.Api.Middleware;

/// <summary>
/// Maps exceptions to RFC 9457 <c>application/problem+json</c> responses.
/// </summary>
/// <remarks>
/// <para>
/// One place decides what an exception looks like on the wire. The alternative — try/catch in each
/// endpoint — drifts the moment anyone adds an endpoint, and produces an API whose error shape
/// depends on which handler failed.
/// </para>
/// <para>
/// The distinction that matters here is between failures the caller can act on and failures they
/// cannot. Validation, not-found, forbidden and conflict get a specific status and a message written
/// for a user. Everything else gets a 500 with the correlation id and nothing else: an unexpected
/// exception message can carry a connection string, a file path or a row of data, and the caller has
/// no use for it. The detail is in the logs, joined by the same correlation id.
/// </para>
/// </remarks>
public sealed class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<ProblemDetailsExceptionHandler> logger)
    : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var failure = Map(exception, environment.IsDevelopment());

        if (failure.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.UnhandledException(exception);
        }
        else if (logger.IsEnabled(LogLevel.Information))
        {
            // Guarded: GetType().Name allocates, and at Warning or above this branch would pay for
            // a string nobody reads. The source-generated delegate cannot elide an argument that is
            // computed at the call site, which is what CA1873 is pointing at.
            logger.HandledFailure(exception.GetType().Name, failure.Status, exception.Message);
        }

        httpContext.Response.StatusCode = failure.Status;

        var problemDetails = new ProblemDetails
        {
            Status = failure.Status,
            Title = failure.Title,
            Detail = failure.Detail,
            Type = $"https://httpstatuses.io/{failure.Status}",
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
        };

        // The correlation id goes in the body as well as the header, so a user can quote it from a
        // screenshot and it can be grepped straight out of the logs.
        problemDetails.Extensions["correlationId"] = httpContext.TraceIdentifier;

        if (failure.Errors is not null)
        {
            problemDetails.Extensions["errors"] = failure.Errors;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }

    private static ApiFailure Map(Exception exception, bool isDevelopment) => exception switch
    {
        ValidationException validation => new ApiFailure(
            StatusCodes.Status400BadRequest,
            "One or more validation errors occurred.",
            "The request did not pass validation.",
            validation.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray())),

        NotFoundException notFound => new ApiFailure(
            StatusCodes.Status404NotFound, "Not found", notFound.Message),

        // 403, not 401. The caller is authenticated; they are simply not allowed. Returning 401
        // would tell a client to re-authenticate, which cannot help.
        ForbiddenException forbidden => new ApiFailure(
            StatusCodes.Status403Forbidden, "Forbidden", forbidden.Message),

        ConflictException conflict => new ApiFailure(
            StatusCodes.Status409Conflict, "Conflict", conflict.Message),

        // A broken business rule is a well-formed request the domain refuses: 422, not 400.
        DomainException domain => new ApiFailure(
            StatusCodes.Status422UnprocessableEntity, "The request violates a domain rule", domain.Message),

        // Reaches here only from a write that did not route through IUnitOfWork's translation.
        DbUpdateConcurrencyException => new ApiFailure(
            StatusCodes.Status409Conflict,
            "Conflict",
            "The record was modified by someone else. Reload and try again."),

        OperationCanceledException => new ApiFailure(
            ClientStatusCodes.ClientClosedRequest, "Client closed request", "The request was cancelled."),

        // Any other application-layer failure that has not been given its own status yet.
        WorkBoardException application => new ApiFailure(
            StatusCodes.Status400BadRequest, "Bad request", application.Message),

        _ => new ApiFailure(
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred",
            isDevelopment ? exception.ToString() : "The error has been logged with the correlation id below."),
    };
}
