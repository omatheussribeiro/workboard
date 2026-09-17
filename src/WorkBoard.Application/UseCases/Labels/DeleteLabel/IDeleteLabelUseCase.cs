namespace WorkBoard.Application.UseCases.Labels.DeleteLabel;

/// <summary>Deletes a label, untagging every card that carried it.</summary>
public interface IDeleteLabelUseCase
{
    /// <summary>Deletes the label.</summary>
    Task ExecuteAsync(Guid labelId, CancellationToken cancellationToken);
}
