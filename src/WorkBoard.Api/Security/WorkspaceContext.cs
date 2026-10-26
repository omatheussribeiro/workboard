using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Api.Security;

/// <summary>
/// The mutable, request-scoped tenant state populated after workspace authorization succeeds.
/// </summary>
/// <remarks>
/// Only the authorization handler calls <see cref="Enter"/>, after checking membership. The
/// Infrastructure DbContext consumes the read-only <see cref="ITenantContext"/> contract, so it
/// does not depend on the HTTP layer that resolves the workspace.
/// </remarks>
public sealed class WorkspaceContext : ITenantContext
{
    public Guid? WorkspaceId { get; private set; }

    /// <summary>The caller's role in the resolved workspace, if any.</summary>
    public WorkspaceRole? Role { get; private set; }

    /// <summary>Binds this request to a workspace after membership has been proven.</summary>
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
