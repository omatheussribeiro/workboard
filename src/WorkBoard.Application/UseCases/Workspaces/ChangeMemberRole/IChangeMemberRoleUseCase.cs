using WorkBoard.Application.DTOs.Workspaces;

namespace WorkBoard.Application.UseCases.Workspaces.ChangeMemberRole;

/// <summary>Changes a member's role inside a workspace.</summary>
public interface IChangeMemberRoleUseCase
{
    /// <summary>Applies the new role.</summary>
    Task ExecuteAsync(
        Guid workspaceId,
        Guid userId,
        ChangeMemberRoleRequest request,
        CancellationToken cancellationToken);
}
