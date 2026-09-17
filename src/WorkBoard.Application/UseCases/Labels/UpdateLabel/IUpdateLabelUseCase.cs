using WorkBoard.Application.DTOs.Labels;

namespace WorkBoard.Application.UseCases.Labels.UpdateLabel;

/// <summary>Renames and recolours a label.</summary>
public interface IUpdateLabelUseCase
{
    /// <summary>Applies the change, or reports a conflict when the new name is taken.</summary>
    Task ExecuteAsync(Guid labelId, UpdateLabelRequest request, CancellationToken cancellationToken);
}
