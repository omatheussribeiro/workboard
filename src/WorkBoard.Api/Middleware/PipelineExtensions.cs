namespace WorkBoard.Api.Middleware;

/// <summary>Registration helpers for the custom middleware.</summary>
public static class PipelineExtensions
{
    /// <summary>Adds <see cref="CorrelationIdMiddleware"/>.</summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    /// <summary>Adds <see cref="SecurityHeadersMiddleware"/>.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
