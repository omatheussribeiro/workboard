namespace WorkBoard.Application.UseCases.Projects.ArchiveProject;

/// <summary>Archives a project, hiding it from the default lists.</summary>
public interface IArchiveProjectUseCase
{
    /// <summary>Archives it. Nothing is deleted.</summary>
    Task ExecuteAsync(Guid projectId, CancellationToken cancellationToken);
}
