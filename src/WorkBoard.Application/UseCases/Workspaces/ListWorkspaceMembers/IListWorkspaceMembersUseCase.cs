using WorkBoard.Application.DTOs.Workspaces;

namespace WorkBoard.Application.UseCases.Workspaces.ListWorkspaceMembers;

/// <summary>Reads the member list of a workspace.</summary>
public interface IListWorkspaceMembersUseCase
{
    /// <summary>Members, owners first.</summary>
    Task<IReadOnlyList<WorkspaceMemberDto>> ExecuteAsync(Guid workspaceId, CancellationToken cancellationToken);
}
