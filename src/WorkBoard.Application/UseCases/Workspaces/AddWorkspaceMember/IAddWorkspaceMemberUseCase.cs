using WorkBoard.Application.DTOs.Workspaces;

namespace WorkBoard.Application.UseCases.Workspaces.AddWorkspaceMember;

/// <summary>Adds an existing account to a workspace.</summary>
public interface IAddWorkspaceMemberUseCase
{
    /// <summary>Adds the member and returns the new membership row.</summary>
    Task<WorkspaceMemberDto> ExecuteAsync(
        Guid workspaceId,
        AddMemberRequest request,
        CancellationToken cancellationToken);
}
