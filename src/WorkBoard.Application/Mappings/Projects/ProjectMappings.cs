using System.Linq.Expressions;
using WorkBoard.Application.DTOs.Projects;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Mappings.Projects;

/// <summary>Entity-to-DTO projections for projects.</summary>
/// <remarks>
/// The two counts are correlated subqueries, so listing twenty projects stays one round trip
/// returning twenty rows — rather than loading every task of every project to call <c>Count()</c> in
/// memory, which is the shape this projection exists to prevent anyone reintroducing.
/// </remarks>
public static class ProjectMappings
{
    /// <summary>Projects a project onto the summary used by the list and the detail header.</summary>
    public static Expression<Func<Project, ProjectSummaryDto>> ToSummary() =>
        project => new ProjectSummaryDto(
            project.Id,
            project.Name,
            project.Key,
            project.Description,
            project.IsArchived,
            project.Tasks.Count,
            project.Tasks.Count(task => task.CompletedAtUtc == null),
            project.Boards.Select(board => (Guid?)board.Id).FirstOrDefault(),
            project.CreatedAtUtc);
}
