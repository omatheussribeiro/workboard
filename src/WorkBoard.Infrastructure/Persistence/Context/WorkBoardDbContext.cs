using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.Services;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Infrastructure.Persistence.Context;

/// <summary>
/// The single <c>DbContext</c> for the whole application.
/// </summary>
/// <remarks>
/// <para>
/// One context, not one per module. Splitting would buy independent model caches and smaller
/// startup, and would cost the ability to save a task, its activity entry and its notifications in
/// one transaction — which is exactly the guarantee this design depends on. A modular monolith with
/// a shared database is the honest shape here; see ADR-004.
/// </para>
/// </remarks>
public sealed class WorkBoardDbContext(
    DbContextOptions<WorkBoardDbContext> options,
    ITenantContext tenant,
    IClock clock,
    DomainEventDispatcher dispatcher)
    : DbContext(options)
{
    /// <summary>
    /// The tenant the query filters compare against.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Falls back to <see cref="Guid.Empty"/> rather than disabling the filter when no workspace is
    /// resolved. That is the whole point: a bug that forgets to set the tenant makes queries return
    /// nothing, instead of returning every tenant's data. Fail closed, never open.
    /// </para>
    /// <para>
    /// EF Core turns a filter that reads a context property into a query parameter evaluated per
    /// execution, so this stays correct across the pooled model cache — the value is not baked into
    /// the compiled query.
    /// </para>
    /// </remarks>
    private Guid CurrentWorkspaceId => tenant.WorkspaceId ?? Guid.Empty;

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Board> Boards => Set<Board>();

    public DbSet<BoardColumn> BoardColumns => Set<BoardColumn>();

    public DbSet<WorkItem> Tasks => Set<WorkItem>();

    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();

    public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Label> Labels => Set<Label>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public IQueryable<T> IgnoringTenantFilter<T>() where T : class => Set<T>().IgnoreQueryFilters();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkBoardDbContext).Assembly);

        ApplyTenantFilters(modelBuilder);
        ApplyConventions(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Applied model-wide so no configuration has to remember it. nvarchar(max) is the default
        // for an unconfigured string, and a max-length column cannot participate in an index.
        configurationBuilder.Properties<string>().HaveMaxLength(256);

        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    /// <summary>
    /// Stamps audit timestamps, dispatches domain events, then saves — all in one transaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Order matters. Events are dispatched <em>before</em> <c>base.SaveChangesAsync</c>, so the
    /// activity and notification rows they produce join the same unit of work and therefore the same
    /// implicit transaction. Dispatching afterwards would let a task be created while its history
    /// entry silently failed.
    /// </para>
    /// <para>
    /// Handlers can raise further events (a notification handler reacting to an assignment), so the
    /// dispatcher drains the queue rather than making a single pass.
    /// </para>
    /// </remarks>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        await dispatcher.DispatchAsync(this, cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = clock.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreated(now);
                    break;

                case EntityState.Modified:
                    entry.Entity.SetUpdated(now);
                    break;

                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Attaches the workspace filter to every entity that declares itself tenant-scoped.
    /// </summary>
    /// <remarks>
    /// Applied by reflecting over <see cref="IWorkspaceScoped"/> rather than by writing
    /// <c>HasQueryFilter</c> in each configuration. The difference is not brevity: a per-entity
    /// filter is something a future entity can be added without, and the omission would be
    /// invisible — a silent cross-tenant leak. Here, implementing the interface is the only thing a
    /// new entity has to do, and an architecture test asserts every tenant-owned entity does.
    /// </remarks>
    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IWorkspaceScoped).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var workspaceId = System.Linq.Expressions.Expression.Property(parameter, nameof(IWorkspaceScoped.WorkspaceId));
            var currentWorkspaceId = System.Linq.Expressions.Expression.Property(
                System.Linq.Expressions.Expression.Constant(this),
                nameof(CurrentWorkspaceId));

            var filter = System.Linq.Expressions.Expression.Lambda(
                System.Linq.Expressions.Expression.Equal(workspaceId, currentWorkspaceId),
                parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    /// <summary>
    /// Model-wide conventions that are easier to get right once than in twelve configurations.
    /// </summary>
    private static void ApplyConventions(ModelBuilder modelBuilder)
    {
        // Guid primary keys are generated by the domain (UUIDv7 in Entity), never by the database, and
        // EF has to be told so explicitly.
        //
        // Without this, EF treats a Guid key as ValueGeneratedOnAdd and infers entity state from the
        // key: an untracked instance reached through a navigation with a key already set looks like
        // an existing row, so EF issues an UPDATE that matches nothing and throws
        // DbUpdateConcurrencyException. The symptom is baffling — "logging in" fails with a
        // concurrency conflict — and the cause is three layers away.
        //
        // Declaring it once here, derived from the model rather than repeated in twelve
        // configurations, means a new entity cannot be added without it.
        foreach (var keyProperty in modelBuilder.Model.GetEntityTypes()
                     .Select(entityType => entityType.FindPrimaryKey())
                     .Where(key => key is not null)
                     .SelectMany(key => key!.Properties)
                     .Where(property => property.ClrType == typeof(Guid)))
        {
            keyProperty.ValueGenerated = ValueGenerated.Never;
        }

        // Every date in the schema is a DateTimeOffset, mapped to datetimeoffset(3). The default
        // precision of 7 buys sub-microsecond resolution nothing in this domain needs, at 10 bytes
        // per column instead of 8.
        //
        // Delete behaviour is deliberately *not* set here. SQL Server rejects multiple cascade paths
        // to the same table, and this schema has several — a task reaches Workspace through both
        // Project and BoardColumn — so each relationship declares its own behaviour in its
        // configuration, where the reasoning is visible next to the relationship it applies to.
        // A blanket loop would also silently overwrite those choices, since it runs after them.
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetProperties())
                     .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?)))
        {
            property.SetPrecision(3);
        }
    }
}
