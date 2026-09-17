using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Projects.ArchiveProject;

/// <inheritdoc cref="IArchiveProjectUseCase"/>
public sealed class ArchiveProjectUseCase(IProjectRepository projects, IUnitOfWork unitOfWork)
    : IArchiveProjectUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.FindForUpdateAsync(projectId, cancellationToken)
                      ?? throw new NotFoundException("Project", projectId);

        project.Archive();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
