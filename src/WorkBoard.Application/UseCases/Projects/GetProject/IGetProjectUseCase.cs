using WorkBoard.Application.DTOs.Projects;

namespace WorkBoard.Application.UseCases.Projects.GetProject;

/// <summary>Reads one project.</summary>
public interface IGetProjectUseCase
{
    /// <summary>Returns the project.</summary>
    /// <exception cref="WorkBoard.Application.Exceptions.NotFoundException">Unknown project, or it belongs to another workspace.</exception>
    Task<ProjectSummaryDto> ExecuteAsync(Guid projectId, CancellationToken cancellationToken);
}
