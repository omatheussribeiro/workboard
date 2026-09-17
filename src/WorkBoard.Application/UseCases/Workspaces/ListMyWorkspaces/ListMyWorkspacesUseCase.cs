using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Workspaces;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.UseCases.Workspaces.ListMyWorkspaces;

/// <inheritdoc cref="IListMyWorkspacesUseCase"/>
/// <remarks>
/// One of only two reads in the application that legitimately cross tenants, and the reason is
/// structural: "which workspaces does this user belong to" cannot be filtered by a workspace that
/// has not been chosen yet. It is anchored on the caller's own user id, which is what makes crossing
/// the filter safe, and it is covered by an isolation test.
/// </remarks>
public sealed class ListMyWorkspacesUseCase(IWorkBoardDbContext db, ICurrentUser currentUser)
    : IListMyWorkspacesUseCase
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkspaceSummaryDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        return await db.IgnoringTenantFilter<WorkspaceMember>()
            .AsNoTracking()
            .Where(membership => membership.UserId == userId && !membership.Workspace.IsArchived)
            .OrderBy(membership => membership.Workspace.Name)
            .Select(WorkspaceMappings.ToSummary())
            .ToListAsync(cancellationToken);
    }
}
