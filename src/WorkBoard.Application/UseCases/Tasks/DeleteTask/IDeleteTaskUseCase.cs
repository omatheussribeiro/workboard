namespace WorkBoard.Application.UseCases.Tasks.DeleteTask;

/// <summary>Deletes a card that has no subtasks left.</summary>
public interface IDeleteTaskUseCase
{
    /// <summary>Deletes it, or reports a conflict when subtasks remain.</summary>
    Task ExecuteAsync(Guid taskId, CancellationToken cancellationToken);
}
