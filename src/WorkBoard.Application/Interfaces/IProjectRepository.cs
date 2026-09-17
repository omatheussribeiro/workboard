using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>Write-side access to the project aggregate.</summary>
public interface IProjectRepository
{
    /// <summary>Loads a project tracked, so its counter and concurrency token take part in the save.</summary>
    Task<Project?> FindForUpdateAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>Loads a project tracked together with its boards.</summary>
    Task<Project?> FindWithBoardsAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Whether the workspace already uses this project key.
    /// </summary>
    /// <remarks>
    /// Checked before insert so the caller gets a sentence instead of a constraint violation, but
    /// the unique index is what actually enforces it: between this read and the insert another
    /// request can win the race. See ADR-005.
    /// </remarks>
    Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken);

    /// <summary>Stages a new project.</summary>
    void Add(Project project);
}
