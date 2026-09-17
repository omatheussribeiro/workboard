using System.Linq.Expressions;
using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Mappings.Workspaces;

/// <summary>
/// Entity-to-DTO projections for workspaces.
/// </summary>
/// <remarks>
/// The summary projection starts from <see cref="WorkspaceMember"/> rather than from
/// <see cref="Workspace"/>, which is what lets <c>MyRole</c> be part of the same query: the row the
/// caller is entitled to see and the role they hold are the same row. The two counts are correlated
/// subqueries, so the whole list is one round trip returning one row per workspace — rather than
/// loading every member and every project to produce two integers.
/// </remarks>
public static class WorkspaceMappings
{
    /// <summary>Projects a membership row onto the workspace summary the client renders.</summary>
    public static Expression<Func<WorkspaceMember, WorkspaceSummaryDto>> ToSummary() =>
        membership => new WorkspaceSummaryDto(
            membership.WorkspaceId,
            membership.Workspace.Name,
            membership.Workspace.Slug,
            membership.Workspace.Description,
            membership.Role,
            membership.Workspace.Members.Count,
            membership.Workspace.Projects.Count(project => !project.IsArchived),
            membership.Workspace.CreatedAtUtc);

    /// <summary>Projects a membership row onto a member list entry.</summary>
    public static Expression<Func<WorkspaceMember, WorkspaceMemberDto>> ToMember() =>
        membership => new WorkspaceMemberDto(
            membership.UserId,
            membership.User.Email,
            membership.User.DisplayName,
            membership.Role,
            membership.JoinedAtUtc);
}
