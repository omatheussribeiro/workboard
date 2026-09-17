
namespace WorkBoard.Domain.Interfaces;

/// <summary>
/// Marks an entity as belonging to exactly one workspace — the tenant boundary.
/// </summary>
/// <remarks>
/// <para>
/// <c>WorkspaceId</c> is denormalised onto every tenant-scoped row, even where it could be derived
/// (a comment → task → project → workspace). That duplication buys two things:
/// </para>
/// <list type="bullet">
///   <item>the EF Core global query filter is one indexed predicate, not a three-table join;</item>
///   <item>a query that forgets to join is still filtered — isolation does not depend on the
///   caller writing the right <c>Where</c>.</item>
/// </list>
/// <para>
/// The cost is that the value must be set on creation and can never change; entities take it as a
/// constructor argument and expose no setter. See ADR-004.
/// </para>
/// </remarks>
public interface IWorkspaceScoped
{
    Guid WorkspaceId { get; }
}
