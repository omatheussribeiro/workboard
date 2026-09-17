using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.Services;

namespace WorkBoard.Infrastructure.Persistence.Context;

/// <summary>
/// Builds a context for the <c>dotnet ef</c> tooling.
/// </summary>
/// <remarks>
/// <para>
/// Without this, <c>dotnet ef migrations add</c> would have to start the API host to obtain a
/// context, which means a valid JWT signing key, a reachable SQL Server and the whole startup path —
/// none of which a schema diff needs. The factory sidesteps that: the tooling only ever asks the
/// model for its shape.
/// </para>
/// <para>
/// The connection string here is used for scaffolding, never to connect during <c>migrations add</c>.
/// It is read from <c>WORKBOARD_MIGRATIONS_CONNECTION</c> when set, so that generating a script
/// against a real database does not require editing this file.
/// </para>
/// </remarks>
public sealed class WorkBoardDbContextFactory : IDesignTimeDbContextFactory<WorkBoardDbContext>
{
    // A verbatim string, so the instance name keeps its single backslash. In a regular literal
    // ".\SQLEXPRESS" is a compile error — \S is not an escape sequence — and ".\\SQLEXPRESS" reads
    // like a typo next to the same value in appsettings.json, where JSON does require the doubling.
    private const string FallbackConnection =
        @"Server=.\SQLEXPRESS;Database=WorkBoard;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=30";

    public WorkBoardDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("WORKBOARD_MIGRATIONS_CONNECTION") ?? FallbackConnection;

        var options = new DbContextOptionsBuilder<WorkBoardDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsAssembly(typeof(WorkBoardDbContext).Assembly.FullName))
            .Options;

        // Design time has no request, so there is no tenant and no events to dispatch. The empty
        // tenant makes the query filters compare against Guid.Empty, which is irrelevant to schema
        // generation but keeps the model identical to the one the application builds.
        return new WorkBoardDbContext(options, new DesignTimeTenantContext(), new DesignTimeClock(), new DomainEventDispatcher([]));
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? WorkspaceId => null;

        public Guid RequireWorkspaceId() => throw new NotSupportedException("No tenant exists at design time.");
    }

    private sealed class DesignTimeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
