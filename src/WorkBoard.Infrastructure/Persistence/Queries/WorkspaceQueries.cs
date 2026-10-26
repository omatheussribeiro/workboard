using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Workspaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

public sealed class WorkspaceQueries(WorkBoardDbContext context) : IWorkspaceQueries
{
    public Task<WorkspaceRole?> GetRoleForUserAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken) =>
        context.IgnoringTenantFilter<WorkspaceMember>()
            .AsNoTracking()
            .Where(membership => membership.WorkspaceId == workspaceId && membership.UserId == userId)
            .Select(membership => (WorkspaceRole?)membership.Role)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkspaceSummaryDto>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await context.IgnoringTenantFilter<WorkspaceMember>()
            .AsNoTracking()
            .Where(membership => membership.UserId == userId && !membership.Workspace.IsArchived)
            .OrderBy(membership => membership.Workspace.Name)
            .Select(WorkspaceMappings.ToSummary())
            .ToListAsync(cancellationToken);

    public Task<WorkspaceSummaryDto?> GetForUserAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken) =>
        context.IgnoringTenantFilter<WorkspaceMember>()
            .AsNoTracking()
            .Where(membership => membership.UserId == userId && membership.WorkspaceId == workspaceId)
            .Select(WorkspaceMappings.ToSummary())
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkspaceMemberDto>> ListMembersAsync(
        Guid workspaceId,
        CancellationToken cancellationToken) =>
        await context.WorkspaceMembers
            .AsNoTracking()
            .Where(membership => membership.WorkspaceId == workspaceId)
            .OrderByDescending(membership => membership.Role)
            .ThenBy(membership => membership.User.DisplayName)
            .Select(WorkspaceMappings.ToMember())
            .ToListAsync(cancellationToken);
}
