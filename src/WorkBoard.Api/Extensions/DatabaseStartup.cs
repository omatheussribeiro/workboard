using WorkBoard.Infrastructure.Persistence.Initialization;

namespace WorkBoard.Api.Extensions;

/// <summary>Applies migrations and seeds demonstration data at startup, when configuration asks for it.</summary>
public static class DatabaseStartup
{
    /// <summary>
    /// Runs the initialiser according to <c>Database:MigrateOnStartup</c> and <c>Database:SeedDemoData</c>.
    /// </summary>
    /// <remarks>
    /// Both are opt-in flags rather than an environment check, so an integration test or a demo
    /// deployment can enable them without pretending to be Development — and so Production cannot
    /// get them by accident, because its settings file sets both to false. See ADR-012 on why
    /// migrating on startup is not the default.
    /// </remarks>
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        var migrate = app.Configuration.GetValue("Database:MigrateOnStartup", false);
        var seed = app.Configuration.GetValue("Database:SeedDemoData", false);

        if (!migrate && !seed)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        if (migrate)
        {
            // Creates the database when it does not exist, and waits for a server that is still
            // starting — which is what makes `docker compose up` work from a cold volume.
            await initializer.MigrateAsync();
        }
        else
        {
            // Seeding without migrating still needs a reachable server; without this the seed throws
            // a connection error while SQL Server is a few seconds from being ready.
            await initializer.WaitForDatabaseAsync();
        }

        if (seed)
        {
            await initializer.SeedAsync();
        }
    }
}
