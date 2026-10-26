using System.Reflection;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.ArchitectureTests;

/// <summary>
/// The conventions the multi-tenancy model depends on, which the compiler cannot enforce.
/// </summary>
/// <remarks>
/// Both tests here exist because breaking the rule produces a bug that looks like something else: a
/// cross-tenant leak reads like a caching problem, and an unfiltered entity fails nothing at all.
/// </remarks>
public sealed class TenantIsolationConventionTests
{
    private static readonly Assembly Domain = typeof(Entity).Assembly;

    /// <summary>
    /// Every entity that belongs to a workspace must declare it.
    /// </summary>
    /// <remarks>
    /// The global query filter is applied by reflecting over <see cref="IWorkspaceScoped"/>, so an
    /// entity that owns tenant data but omits the interface gets no filter at all — and nothing
    /// fails. This test is the thing that would fail.
    /// </remarks>
    [Fact]
    public void Entities_holding_a_WorkspaceId_declare_IWorkspaceScoped()
    {
        var offenders = Domain.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })

            // Domain events also carry a WorkspaceId and are records rather than entities — they are
            // messages about a change, never rows, so no query filter applies to them.
            .Where(type => !typeof(IDomainEvent).IsAssignableFrom(type))
            .Where(type => type.GetMethod(
                "<Clone>$",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is null)
            .Where(type => type.GetProperty("WorkspaceId") is not null)
            .Where(type => !typeof(IWorkspaceScoped).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToList();

        offenders.Should().BeEmpty(
            "an entity with a WorkspaceId that does not implement IWorkspaceScoped is invisible to the tenant filter");
    }

    /// <summary>
    /// Only the places that legitimately need it may bypass the tenant filter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Authentication (no tenant is known yet) and workspace resolution (choosing the tenant) are
    /// the only operations that can run unfiltered. Anything else calling
    /// <c>IgnoringTenantFilter</c> or <c>IgnoreQueryFilters</c> is either a bug or a decision that
    /// deserves this list to be edited and reviewed.
    /// </para>
    /// <para>
    /// The list is short and each entry names a reason. That it can be read in ten seconds is the
    /// property worth protecting: an allow-list nobody can review is not a control.
    /// </para>
    /// </remarks>
    [Fact]
    public void Only_auth_and_workspace_resolution_bypass_the_tenant_filter()
    {
        var allowed = new[]
        {
            // Reads users and refresh tokens, which exist before any workspace does.
            "src/WorkBoard.Infrastructure/Persistence/Repositories/UserRepository.cs",

            // Checks slug uniqueness, which is global by definition.
            "src/WorkBoard.Infrastructure/Persistence/Repositories/WorkspaceRepository.cs",

            // Answer "which workspaces does this user belong to", which cannot be workspace-scoped.
            "src/WorkBoard.Infrastructure/Persistence/Queries/WorkspaceQueries.cs",

            // Declare the escape hatch rather than use it.
            "src/WorkBoard.Infrastructure/Persistence/Context/WorkBoardDbContext.cs",

            // Checks for an empty database before seeding, before any tenant exists.
            "src/WorkBoard.Infrastructure/Persistence/Initialization/DatabaseInitializer.cs",
        };

        var callers = FilterBypassScanner.FindCallers().ToList();

        callers.Except(allowed).Should().BeEmpty(
            "bypassing the workspace filter is how cross-tenant leaks happen; add to the allow-list only with review");
    }
}
