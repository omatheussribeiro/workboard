using WorkBoard.Application.DTOs.Labels;

namespace WorkBoard.Application.UseCases.Labels.ListLabels;

/// <summary>Reads the workspace label taxonomy.</summary>
public interface IListLabelsUseCase
{
    /// <summary>Every label in the workspace, ordered by name.</summary>
    Task<IReadOnlyList<LabelDto>> ExecuteAsync(CancellationToken cancellationToken);
}
