using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Labels;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Labels;

namespace WorkBoard.Application.UseCases.Labels.ListLabels;

/// <inheritdoc cref="IListLabelsUseCase"/>
public sealed class ListLabelsUseCase(IWorkBoardDbContext db) : IListLabelsUseCase
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<LabelDto>> ExecuteAsync(CancellationToken cancellationToken) =>
        await db.Labels
            .AsNoTracking()
            .OrderBy(label => label.Name)
            .Select(LabelMappings.ToDto())
            .ToListAsync(cancellationToken);
}
