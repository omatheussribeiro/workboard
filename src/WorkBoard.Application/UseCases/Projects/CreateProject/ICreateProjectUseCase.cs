using WorkBoard.Application.DTOs.Projects;

namespace WorkBoard.Application.UseCases.Projects.CreateProject;

/// <summary>Creates a project together with its default board and columns.</summary>
public interface ICreateProjectUseCase
{
    /// <summary>Creates the project, or reports a conflict when the key is taken.</summary>
    Task<ProjectSummaryDto> ExecuteAsync(CreateProjectRequest request, CancellationToken cancellationToken);
}
