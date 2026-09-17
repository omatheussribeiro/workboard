using FluentValidation;

namespace WorkBoard.Api.Filters;

/// <summary>
/// Validates the request body of a minimal-API endpoint before the handler runs.
/// </summary>
/// <remarks>
/// <para>
/// Minimal APIs have no equivalent of MVC's <c>ModelState</c>, so validation is an explicit endpoint
/// filter. Adding it per endpoint with <c>.WithValidation&lt;T&gt;()</c> is more typing than a
/// global pipeline behaviour, and is the point: the signature says what is validated, and an
/// endpoint whose body is not validated is visibly missing the call rather than silently relying on
/// a convention.
/// </para>
/// <para>
/// The validator is resolved from DI per request. It is registered as a singleton — FluentValidation
/// validators are stateless — so this is a dictionary lookup, not a construction.
/// </para>
/// </remarks>
/// <typeparam name="T">The request body type to validate.</typeparam>
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
    where T : class
{
    /// <summary>Validates, then continues down the filter chain.</summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            // Model binding failed or the parameter is absent. Nothing to validate, and the failure
            // has already been reported by the binder.
            return await next(context);
        }

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            // Thrown rather than returned, so validation failures take the same path through
            // ProblemDetailsExceptionHandler as every other failure and come back in one shape.
            throw new ValidationException(result.Errors);
        }

        return await next(context);
    }
}
