using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="ILabelRepository"/>
public sealed class LabelRepository(WorkBoardDbContext context) : ILabelRepository
{
    /// <inheritdoc/>
    public Task<Label?> FindForUpdateAsync(Guid labelId, CancellationToken cancellationToken) =>
        context.Labels.FirstOrDefaultAsync(label => label.Id == labelId, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Label>> ListByIdsAsync(
        IReadOnlyCollection<Guid> labelIds,
        CancellationToken cancellationToken)
    {
        if (labelIds.Count == 0)
        {
            return [];
        }

        var ids = labelIds as Guid[] ?? [.. labelIds];

        return await context.Labels
            .Where(label => ids.Contains(label.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        context.Labels.AnyAsync(
            label => label.Name == name && (exceptId == null || label.Id != exceptId),
            cancellationToken);

    /// <inheritdoc/>
    public void Add(Label label) => context.Labels.Add(label);

    /// <inheritdoc/>
    public void Remove(Label label) => context.Labels.Remove(label);
}
