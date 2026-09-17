using Microsoft.Extensions.Logging;

namespace WorkBoard.Infrastructure.Persistence.Initialization;

/// <summary>Source-generated log messages for <see cref="DatabaseInitializer"/>.</summary>
/// <remarks>
/// In its own file, like every other type in this project. The generator emits a cached delegate per
/// message, so the arguments are neither boxed nor formatted when the level is disabled.
/// </remarks>
internal static partial class DatabaseInitializerLog
{
    [LoggerMessage(EventId = 5000, Level = LogLevel.Information, Message = "Database schema is up to date")]
    public static partial void DatabaseUpToDate(this ILogger logger);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Applying {Count} pending migration(s)")]
    public static partial void ApplyingMigrations(this ILogger logger, int count);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Seed skipped: the database already contains data")]
    public static partial void SeedSkipped(this ILogger logger);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information, Message = "Demonstration data seeded")]
    public static partial void SeedCompleted(this ILogger logger);

    [LoggerMessage(
        EventId = 5004,
        Level = LogLevel.Information,
        Message = "Database not reachable yet (attempt {Attempt} of {Attempts}): {Reason}")]
    public static partial void WaitingForDatabase(this ILogger logger, int attempt, int attempts, string reason);

    [LoggerMessage(
        EventId = 5005,
        Level = LogLevel.Warning,
        Message = "Database still not reachable after {Attempts} attempts; letting the migration report the cause")]
    public static partial void DatabaseUnreachable(this ILogger logger, int attempts);
}
