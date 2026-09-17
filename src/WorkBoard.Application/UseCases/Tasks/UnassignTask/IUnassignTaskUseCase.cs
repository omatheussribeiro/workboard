namespace WorkBoard.Application.UseCases.Tasks.UnassignTask;

/// <summary>Removes an assignment from a card.</summary>
public interface IUnassignTaskUseCase
{
    /// <summary>Removes it. Unassigning someone who is not assigned is a no-op.</summary>
    Task ExecuteAsync(Guid taskId, Guid userId, CancellationToken cancellationToken);
}
