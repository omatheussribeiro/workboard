namespace WorkBoard.Application.UseCases.Projects.RestoreProject;

/// <summary>Restores an archived project.</summary>
public interface IRestoreProjectUseCase
{
    /// <summary>Brings the project back into the default lists.</summary>
    Task ExecuteAsync(Guid projectId, CancellationToken cancellationToken);
}
