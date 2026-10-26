using WorkBoard.Application.DTOs.Labels;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Labels.ListLabels;

/// <inheritdoc cref="IListLabelsUseCase"/>
public sealed class ListLabelsUseCase(ILabelQueries queries) : IListLabelsUseCase
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<LabelDto>> ExecuteAsync(CancellationToken cancellationToken) =>
        queries.ListAsync(cancellationToken);
}
