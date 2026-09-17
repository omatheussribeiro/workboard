using WorkBoard.Application.DTOs.Projects;

namespace WorkBoard.Application.UseCases.Projects.UpdateProject;

/// <summary>Renames a project and updates its description.</summary>
public interface IUpdateProjectUseCase
{
    /// <summary>Applies the change.</summary>
    Task ExecuteAsync(Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken);
}
