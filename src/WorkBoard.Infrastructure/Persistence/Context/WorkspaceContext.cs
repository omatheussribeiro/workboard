using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Infrastructure.Persistence.Context;

/// <summary>
/// The mutable, request-scoped implementation of <see cref="ITenantContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Writable, which deserves justification because an ambient mutable value is usually a smell. The
/// alternative would be to resolve the workspace inside the DbContext factory, which would mean the
/// persistence layer parsing routes and reading HTTP headers. Instead exactly one component —
/// <c>WorkspaceContextMiddleware</c> — calls <see cref="Enter"/>, after it has verified membership,
/// and an architecture test asserts nothing else does.
/// </para>
/// <para>
/// Scoped, so the value belongs to one request and cannot leak into another through a pooled object.
/// </para>
/// </remarks>
public sealed class WorkspaceContext : ITenantContext
{
    public Guid? WorkspaceId { get; private set; }

    /// <summary>
    /// The caller role in <see cref="WorkspaceId"/>, resolved alongside it.
    /// </summary>
    /// <remarks>
    /// Kept here rather than as a JWT claim so that removing someone from a workspace takes effect
    /// on their next request instead of when their access token expires. Kept off
    /// <see cref="ITenantContext"/> because persistence needs the tenant, not the role.
    /// </remarks>
    public WorkspaceRole? Role { get; private set; }

    /// <summary>
    /// Binds the request to a workspace. Callers must have already proven membership.
    /// </summary>
    /// <remarks>
    /// Refuses to change an already-set value. A second call would mean two different workspaces
    /// within one request, and any entity loaded before the switch would already have been read
    /// under the previous filter.
    /// </remarks>
    public void Enter(Guid workspaceId, WorkspaceRole role)
    {
        if (WorkspaceId is { } current && current != workspaceId)
        {
            throw new InvalidOperationException("The workspace for this scope has already been set.");
        }

        WorkspaceId = workspaceId;
        Role = role;
    }

    public Guid RequireWorkspaceId() =>
        WorkspaceId ?? throw new InvalidOperationException(
            "This operation requires a workspace. The endpoint is missing the workspace route segment or the WorkspaceMember policy.");
}
