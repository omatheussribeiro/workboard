namespace WorkBoard.Application.UseCases.Tasks.RemoveTaskLabel;

/// <summary>Removes a label from a card.</summary>
public interface IRemoveTaskLabelUseCase
{
    /// <summary>Removes it. Removing a label that is not applied is a no-op.</summary>
    Task ExecuteAsync(Guid taskId, Guid labelId, CancellationToken cancellationToken);
}
