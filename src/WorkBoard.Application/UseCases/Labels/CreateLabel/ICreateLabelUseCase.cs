using WorkBoard.Application.DTOs.Labels;

namespace WorkBoard.Application.UseCases.Labels.CreateLabel;

/// <summary>Creates a workspace label.</summary>
public interface ICreateLabelUseCase
{
    /// <summary>Creates the label, or reports a conflict when the name is taken.</summary>
    Task<LabelDto> ExecuteAsync(CreateLabelRequest request, CancellationToken cancellationToken);
}
