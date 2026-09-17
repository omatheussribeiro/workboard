using WorkBoard.Api.Middleware;

namespace WorkBoard.Api.Extensions;

/// <summary>Registers the browser origin policy.</summary>
public static class CorsRegistration
{
    /// <summary>Name of the single CORS policy this API applies.</summary>
    public const string PolicyName = "workboard-web";

    /// <summary>
    /// Allows exactly the origins listed in <c>Cors:AllowedOrigins</c>.
    /// </summary>
    /// <remarks>
    /// Explicit origins from configuration. <c>AllowAnyOrigin</c> cannot be combined with
    /// credentials, and reflecting the request origin back is equivalent to having no policy at all.
    /// An empty list means no browser origin is allowed, which is the right default for an API
    /// deployed without a known front end.
    /// </remarks>
    public static IServiceCollection AddWorkBoardCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

        return services;
    }
}
