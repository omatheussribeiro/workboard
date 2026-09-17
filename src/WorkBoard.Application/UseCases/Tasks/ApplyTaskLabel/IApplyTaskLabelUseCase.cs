namespace WorkBoard.Application.UseCases.Tasks.ApplyTaskLabel;

/// <summary>Applies a workspace label to a card.</summary>
public interface IApplyTaskLabelUseCase
{
    /// <summary>Applies it. Applying the same label twice is a no-op.</summary>
    Task ExecuteAsync(Guid taskId, Guid labelId, CancellationToken cancellationToken);
}
