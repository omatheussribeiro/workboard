using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.IntegrationTests;

/// <summary>
/// Boots the real API against a real SQL Server.
/// </summary>
/// <remarks>
/// <para>
/// A real database, not the in-memory provider. Everything these tests are for — global query
/// filters, <c>rowversion</c> concurrency, filtered and clustered indexes, cascade behaviour — either
/// does not exist in the in-memory provider or behaves differently there. A suite that passes against
/// it would prove nothing about the thing being claimed.
/// </para>
/// <para>
/// The server comes from Testcontainers by default, which is what CI uses. When
/// <c>WORKBOARD_TEST_CONNECTION</c> is set the tests use that server instead, so a developer with
/// LocalDB or an existing instance can run the suite without Docker. Either way each run gets its own
/// freshly migrated database and drops it afterwards.
/// </para>
/// </remarks>
public sealed class WorkBoardApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestSigningKey = "integration-test-signing-key-at-least-32-chars";

    private readonly MsSqlContainer? _container;
    private readonly string? _externalConnection;
    private readonly string _databaseName = $"WorkBoard_Test_{Guid.NewGuid():N}";

    private string _connectionString = string.Empty;

    public WorkBoardApiFactory()
    {
        _externalConnection = Environment.GetEnvironmentVariable("WORKBOARD_TEST_CONNECTION");

        if (_externalConnection is null)
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        }
    }

    public async Task InitializeAsync()
    {
        if (_container is not null)
        {
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }
        else
        {
            // Point the supplied server at a database name unique to this run, so concurrent runs
            // and leftovers from a previous one cannot interfere.
            _connectionString = new SqlConnectionStringBuilder(_externalConnection!)
            {
                InitialCatalog = _databaseName,
            }.ConnectionString;
        }

        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<WorkBoardDbContext>();

        // Migrate rather than EnsureCreated. EnsureCreated builds the schema from the model and skips
        // the migrations entirely, so a broken migration would pass every test and fail on deploy.
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
        else
        {
            await DropExternalDatabaseAsync();
        }

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString,
                ["Jwt:SigningKey"] = TestSigningKey,
                ["Jwt:Issuer"] = "workboard",
                ["Jwt:Audience"] = "workboard-web",

                // The tests build exactly the data each one needs. Seeding would make them depend on
                // fixtures they did not create, which is how a suite becomes order-dependent.
                ["Database:MigrateOnStartup"] = "false",
                ["Database:SeedDemoData"] = "false",
                ["OpenApi:Enabled"] = "true",

                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",

                // The suite drives hundreds of registrations and logins from one apparent client,
                // which the production auth limit exists to stop. RateLimitingTests lowers these
                // again to prove the limiter still works.
                ["RateLimiting:AuthPerMinute"] = "100000",
                ["RateLimiting:GlobalPerMinute"] = "100000",
                ["RateLimiting:SearchBurst"] = "100000",
                ["RateLimiting:SearchPerTenSeconds"] = "100000",
            }));

        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }

    private async Task DropExternalDatabaseAsync()
    {
        var master = new SqlConnectionStringBuilder(_externalConnection!) { InitialCatalog = "master" }.ConnectionString;

        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();

        // SINGLE_USER WITH ROLLBACK IMMEDIATE: the pooled connections from the test run are still
        // open, and DROP DATABASE refuses while anything is connected.
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID('{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END
            """;

        await command.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Shares one database and one host across every test in the collection.
/// </summary>
/// <remarks>
/// Starting a SQL Server container per test class would add tens of seconds each. The tests are
/// written to be independent instead: each creates its own users and workspace, and the tenant filter
/// means data from one is invisible to another.
/// </remarks>
#pragma warning disable CA1711 // "Collection" is xUnit's term for this concept, not a data structure.
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<WorkBoardApiFactory>
{
    public const string Name = "workboard-api";
}
#pragma warning restore CA1711
