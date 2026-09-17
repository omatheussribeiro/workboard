using WorkBoard.Application.DTOs.Projects;

namespace WorkBoard.Application.UseCases.Projects.ListProjects;

/// <summary>Lists the projects in the current workspace.</summary>
public interface IListProjectsUseCase
{
    /// <summary>Every project, ordered by name.</summary>
    /// <param name="includeArchived">Archived projects are excluded unless this is set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ProjectSummaryDto>> ExecuteAsync(bool includeArchived, CancellationToken cancellationToken);
}
