using Scalar.AspNetCore;
using Serilog;
using WorkBoard.Api.Endpoints;
using WorkBoard.Api.Middleware;

namespace WorkBoard.Api.Extensions;

/// <summary>Builds the HTTP pipeline.</summary>
public static class RequestPipeline
{
    /// <summary>
    /// Adds the middleware, in the order it has to run.
    /// </summary>
    /// <remarks>
    /// Order is load-bearing here, which is the reason this lives in one named method rather than
    /// scattered through <c>Program</c>: the exception handler has to wrap everything below it, the
    /// correlation id has to be set before anything logs, and the rate limiter has to run before
    /// authentication so that an unauthenticated flood is rejected before it costs a signature
    /// validation and a database round trip.
    /// </remarks>
    public static WebApplication UseWorkBoardPipeline(this WebApplication app)
    {
        var openApiEnabled = app.Configuration.GetValue<bool>("OpenApi:Enabled");

        app.UseExceptionHandler();
        app.UseCorrelationId();
        app.UseSecurityHeaders();
        app.UseRateLimiter();

        app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnostic, context) =>
        {
            diagnostic.Set("CorrelationId", context.TraceIdentifier);
            diagnostic.Set(
                "UserId",
                context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        });

        app.UseCors(CorsRegistration.PolicyName);

        // Swagger UI is middleware rather than an endpoint, so it must run before the fallback
        // authorization policy. Scalar and the JSON document are endpoints and can opt out with
        // AllowAnonymous below; Swagger UI has no endpoint metadata on which to put that attribute.
        if (openApiEnabled)
        {
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint("/openapi/v1.json", "WorkBoard API v1");
                options.DocumentTitle = "WorkBoard API — Swagger UI";
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        if (openApiEnabled)
        {
            // Both need AllowAnonymous, and the reason is the FallbackPolicy above: it requires an
            // authenticated user for every endpoint that does not opt out, which is what makes a new
            // route fail closed. The API reference is a route like any other, so without this it
            // answers 401 — the page stays blank and it reads like Scalar is broken rather than like
            // authorization working as designed.
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference(options => options.WithTitle("WorkBoard API")).AllowAnonymous();
        }

        app.MapWorkBoardEndpoints();

        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapGet(
                "/",
                () => openApiEnabled
                    ? Results.Redirect("/scalar/v1")
                    : Results.Ok(new { name = "WorkBoard API", status = "running" }))
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }
}
