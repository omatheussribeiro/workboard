using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Workspaces;

namespace WorkBoard.Application.UseCases.Workspaces.ListWorkspaceMembers;

/// <inheritdoc cref="IListWorkspaceMembersUseCase"/>
/// <remarks>
/// Unlike the two workspace-listing reads, this one does <em>not</em> cross the tenant filter: by the
/// time it runs a workspace has been resolved and membership proven, so the ordinary filtered set is
/// both correct and sufficient.
/// </remarks>
public sealed class ListWorkspaceMembersUseCase(IWorkBoardDbContext db) : IListWorkspaceMembersUseCase
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkspaceMemberDto>> ExecuteAsync(
        Guid workspaceId,
        CancellationToken cancellationToken) =>
        await db.WorkspaceMembers
            .AsNoTracking()
            .Where(membership => membership.WorkspaceId == workspaceId)
            .OrderByDescending(membership => membership.Role)
            .ThenBy(membership => membership.User.DisplayName)
            .Select(WorkspaceMappings.ToMember())
            .ToListAsync(cancellationToken);
}
