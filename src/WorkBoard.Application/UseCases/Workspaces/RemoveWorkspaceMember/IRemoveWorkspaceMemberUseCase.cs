namespace WorkBoard.Application.UseCases.Workspaces.RemoveWorkspaceMember;

/// <summary>Removes a member from a workspace.</summary>
public interface IRemoveWorkspaceMemberUseCase
{
    /// <summary>Removes them. The owner cannot be removed.</summary>
    Task ExecuteAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);
}
