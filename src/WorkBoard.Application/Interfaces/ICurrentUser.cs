using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.Interfaces;

/// <summary>The authenticated principal, projected to just what the application layer needs.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsSystemAdmin { get; }

    /// <summary>Role inside the workspace resolved for this request, if any.</summary>
    WorkspaceRole? WorkspaceRole { get; }

    Guid RequireUserId();

    bool HasAtLeast(WorkspaceRole role) => WorkspaceRole is { } actual && actual >= role;
}
