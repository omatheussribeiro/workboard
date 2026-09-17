namespace WorkBoard.Api.Extensions;

/// <summary>
/// Tunable rate limits. The defaults are the production values.
/// </summary>
/// <remarks>
/// Configuration rather than constants for two reasons: the right number depends on the deployment
/// (one replica or six, behind a NAT or not), and the integration suite drives hundreds of logins
/// from a single apparent address — exactly what the production limit exists to stop. The suite
/// raises them, and one test lowers them again to prove the limiter still rejects.
/// </remarks>
public sealed class RateLimitOptions
{
    /// <summary>Configuration section these values are bound from.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Anonymous auth calls per minute, per IP. Low, because each one runs BCrypt.</summary>
    public int AuthPerMinute { get; init; } = 10;

    /// <summary>Burst capacity for the search box, per user.</summary>
    public int SearchBurst { get; init; } = 20;

    /// <summary>Sustained search rate, per user.</summary>
    public int SearchPerTenSeconds { get; init; } = 10;

    /// <summary>The catch-all limit, per user or IP.</summary>
    public int GlobalPerMinute { get; init; } = 300;
}
