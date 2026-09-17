using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Initialization;

/// <summary>
/// Waits for the database server, brings the schema up to date, and — in Development only — fills it
/// with demonstration data.
/// </summary>
/// <remarks>
/// <para>
/// <c>Migrate()</c> is called from here and only ever for Development, driven by the
/// <c>Database:MigrateOnStartup</c> flag which the Production settings file sets to <c>false</c>. It
/// creates the database when it does not exist yet, which is what makes <c>docker compose up</c> and
/// a bare local SQL Server instance both work with no manual step.
/// </para>
/// <para>
/// Migrating on startup in production is a trap worth naming, because it is the default in so many
/// samples. It takes a schema lock during a rolling deploy while old instances are still serving; it
/// runs once per replica, so several instances race for the same migration; it gives the process
/// running the application permission to alter schema, which it otherwise would not need; and a
/// failed migration becomes a crash loop rather than a deployment that stopped. The pipeline instead
/// generates an idempotent script (<c>dotnet ef migrations script --idempotent</c>) and applies it
/// as a separate, reviewable step before the new version rolls out. See ADR-012.
/// </para>
/// </remarks>
public sealed class DatabaseInitializer(WorkBoardDbContext context, ILogger<DatabaseInitializer> logger)
{
    /// <summary>How many times to retry while the server is still starting.</summary>
    private const int ConnectionAttempts = 12;

    /// <summary>Gap between attempts. Twelve attempts five seconds apart covers a cold SQL Server container.</summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Blocks until the server accepts connections, or gives up after <see cref="ConnectionAttempts"/> tries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The compose health check already gates the API container on SQL Server reporting healthy, but
    /// "answers SELECT 1" and "is ready for a schema migration" are not quite the same moment, and
    /// outside compose — a developer running <c>dotnet run</c> against an instance that is still
    /// starting — there is no gate at all. Without this the first migration throws a connection
    /// error and the host exits, which reads like a configuration problem rather than a timing one.
    /// </para>
    /// <para>
    /// It catches <see cref="SqlException"/> specifically. Catching everything would swallow a wrong
    /// password or an unknown host and retry it twelve times before failing with the same error a
    /// minute later, which is strictly worse than failing immediately.
    /// </para>
    /// </remarks>
    public async Task WaitForDatabaseAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= ConnectionAttempts; attempt++)
        {
            try
            {
                if (await context.Database.CanConnectAsync(cancellationToken))
                {
                    return;
                }
            }
            catch (SqlException exception) when (attempt < ConnectionAttempts)
            {
                logger.WaitingForDatabase(attempt, ConnectionAttempts, exception.Message);
            }

            if (attempt < ConnectionAttempts)
            {
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }

        // Falls through to Migrate, which will throw with the provider's own diagnostics — a better
        // message than anything this method could invent about why the server is unreachable.
        logger.DatabaseUnreachable(ConnectionAttempts);
    }

    /// <summary>
    /// Creates the database if it is missing and applies any pending migrations.
    /// </summary>
    /// <remarks>
    /// <c>MigrateAsync</c> creates the database when it does not exist, so there is no separate
    /// "create" step and no <c>EnsureCreated</c> anywhere in the codebase — the two are mutually
    /// exclusive, and <c>EnsureCreated</c> builds a schema that the migration history knows nothing
    /// about, so the first migration afterwards fails.
    /// </remarks>
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await WaitForDatabaseAsync(cancellationToken);

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            logger.DatabaseUpToDate();
            return;
        }

        logger.ApplyingMigrations(pending.Count);
        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Seeds the demonstration workspace, unless one is already there.
    /// </summary>
    /// <remarks>
    /// Idempotent by checking for existing data first, so restarting the container does not
    /// accumulate duplicate workspaces. The seed writes through the domain factories rather than
    /// through raw SQL, which means the seeded data has been through the same invariants as anything
    /// a user creates — and a change that breaks those invariants fails at startup instead of
    /// producing data the application cannot load.
    /// </remarks>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Users.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            logger.SeedSkipped();
            return;
        }

        await DemoDataSeeder.SeedAsync(context, cancellationToken);
        logger.SeedCompleted();
    }
}
