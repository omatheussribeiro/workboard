using WorkBoard.Application.DTOs.Projects;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Projects.UpdateProject;

/// <inheritdoc cref="IUpdateProjectUseCase"/>
public sealed class UpdateProjectUseCase(IProjectRepository projects, IUnitOfWork unitOfWork)
    : IUpdateProjectUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(
        Guid projectId,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projects.FindForUpdateAsync(projectId, cancellationToken)
                      ?? throw new NotFoundException("Project", projectId);

        project.Rename(request.Name);
        project.Describe(request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
