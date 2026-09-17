using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Projects.RestoreProject;

/// <inheritdoc cref="IRestoreProjectUseCase"/>
public sealed class RestoreProjectUseCase(IProjectRepository projects, IUnitOfWork unitOfWork)
    : IRestoreProjectUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.FindForUpdateAsync(projectId, cancellationToken)
                      ?? throw new NotFoundException("Project", projectId);

        project.Restore();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
