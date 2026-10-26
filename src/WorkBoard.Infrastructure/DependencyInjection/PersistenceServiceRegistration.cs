using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.Persistence.Context;
using WorkBoard.Infrastructure.Persistence.Initialization;
using WorkBoard.Infrastructure.Persistence.Queries;
using WorkBoard.Infrastructure.Persistence.Repositories;
using WorkBoard.Infrastructure.Services;

namespace WorkBoard.Infrastructure.DependencyInjection;

/// <summary>Registers the <c>DbContext</c>, query adapters, repositories and unit of work.</summary>
public static class PersistenceServiceRegistration
{
    /// <summary>Adds everything that talks to SQL Server.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WorkBoardDbContext>(options => ConfigureSqlServer(options, configuration));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddRepositories();
        services.AddQueries();

        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<IDomainEventListener, ActivityWriter>();
        services.AddScoped<IDomainEventListener, NotificationWriter>();

        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    /// <summary>
    /// Registers the write-side repositories.
    /// </summary>
    /// <remarks>
    /// Listed one by one rather than discovered by assembly scanning. There are eight of them, the
    /// list changes a few times a year, and an explicit registration fails at compile time when an
    /// interface is renamed — whereas a scan fails at the first request, in a different file, with a
    /// message about a missing service rather than about the rename that caused it.
    /// </remarks>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }

    private static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddScoped<IActivityQueries, ActivityQueries>();
        services.AddScoped<IBoardQueries, BoardQueries>();
        services.AddScoped<ICommentQueries, CommentQueries>();
        services.AddScoped<IDashboardQueries, DashboardQueries>();
        services.AddScoped<ILabelQueries, LabelQueries>();
        services.AddScoped<INotificationQueries, NotificationQueries>();
        services.AddScoped<IProjectQueries, ProjectQueries>();
        services.AddScoped<ISearchQueries, SearchQueries>();
        services.AddScoped<ITaskQueries, TaskQueries>();
        services.AddScoped<IWorkspaceQueries, WorkspaceQueries>();

        return services;
    }

    /// <summary>
    /// Points EF Core at SQL Server and pins the behaviours this codebase depends on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lazy loading is deliberately absent: the Proxies package is not referenced at all, which is a
    /// stronger statement than disabling it. Lazy loading turns a forgotten <c>Include</c> into a
    /// silent query per row — the N+1 that makes EF Core look slow — and it does so at render time,
    /// far from the code responsible. Every load here is explicit: an <c>Include</c> on the write
    /// path, a projection on the read path.
    /// </para>
    /// <para>
    /// Tracking is left at the EF Core default (<c>TrackAll</c>) deliberately, and it is worth
    /// saying why, because flipping it to <c>NoTracking</c> globally looks like a free performance
    /// win. It is not: <c>SaveChanges</c> persists what the change tracker knows about, so a global
    /// <c>NoTracking</c> default silently turns every write path into a no-op — the entity loads,
    /// the domain method runs, <c>SaveChanges</c> reports success, and nothing reaches the database.
    /// The failure is invisible in a unit test that never saves. The cost is paid on the read side
    /// instead, where it is visible and local: every read query either projects to a DTO, which
    /// tracks nothing anyway, or says <c>AsNoTracking</c> explicitly.
    /// </para>
    /// </remarks>
    private static void ConfigureSqlServer(DbContextOptionsBuilder options, IConfiguration configuration) =>
        options.UseSqlServer(
            configuration.GetConnectionString("Default"),
            sql =>
            {
                sql.MigrationsAssembly(typeof(WorkBoardDbContext).Assembly.FullName);

                // SQL Server in a container, and Azure SQL in particular, drops connections for
                // reasons that have nothing to do with the query. The built-in execution strategy
                // retries those and only those; it is not a retry-everything wrapper.
                //
                // It has one consequence worth knowing: with retries enabled EF refuses an
                // explicitly opened transaction, because it cannot replay a block of user code.
                // Anything that needs one goes through IUnitOfWork.ExecuteInTransactionAsync.
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);

                sql.CommandTimeout(30);
            });
}
