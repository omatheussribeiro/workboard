using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.Interfaces;

public interface IWorkspaceQueries
{
    Task<WorkspaceRole?> GetRoleForUserAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<WorkspaceSummaryDto?> GetForUserAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkspaceSummaryDto>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkspaceMemberDto>> ListMembersAsync(
        Guid workspaceId,
        CancellationToken cancellationToken);
}
