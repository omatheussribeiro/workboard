using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>
/// The persistence surface the application layer is allowed to use.
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately <em>not</em> a repository. It exposes <see cref="DbSet{T}"/> directly, so
/// handlers keep the full expressiveness of LINQ: projections, grouping, paging and split queries
/// all compose and translate to a single round trip. A <c>IRepository&lt;T&gt;</c> facade would
/// re-implement a worse version of that and force in-memory work for anything it did not anticipate.
/// </para>
/// <para>
/// What the interface buys, given that, is narrow but real: the application layer does not
/// reference the SQL Server provider or the concrete context, the architecture tests can enforce
/// that, and tests can substitute a context without a running database. See ADR-001.
/// </para>
/// </remarks>
public interface IWorkBoardDbContext
{
    DbSet<User> Users { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Workspace> Workspaces { get; }

    DbSet<WorkspaceMember> WorkspaceMembers { get; }

    DbSet<Project> Projects { get; }

    DbSet<Board> Boards { get; }

    DbSet<BoardColumn> BoardColumns { get; }

    DbSet<WorkItem> Tasks { get; }

    DbSet<TaskAssignment> TaskAssignments { get; }

    DbSet<TaskLabel> TaskLabels { get; }

    DbSet<Comment> Comments { get; }

    DbSet<Label> Labels { get; }

    DbSet<Activity> Activities { get; }

    DbSet<Notification> Notifications { get; }

    /// <summary>Transactions and the execution strategy, for the few operations that need them.</summary>
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a query with the workspace filter suspended.
    /// </summary>
    /// <remarks>
    /// Needed for exactly two things: authenticating a user before any tenant is known, and
    /// resolving which workspaces a user belongs to. Every call site is covered by an isolation
    /// test, and the architecture tests keep the method out of the API layer.
    /// </remarks>
    IQueryable<T> IgnoringTenantFilter<T>() where T : class;
}
