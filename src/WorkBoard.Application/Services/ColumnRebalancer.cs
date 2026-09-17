using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.Services;

/// <inheritdoc cref="IColumnRebalancer"/>
/// <remarks>
/// Runs only when a move reported <c>WorkItem.NeedsRebalance</c> — the rare case where repeated
/// midpoint insertions have exhausted the gap between two doubles. It is the one place the project
/// accepts an O(n) write, and it is bounded by the cards in a single column.
/// </remarks>
public sealed class ColumnRebalancer(ITaskRepository tasks, IUnitOfWork unitOfWork) : IColumnRebalancer
{
    /// <inheritdoc/>
    public async Task RebalanceAsync(Guid columnId, CancellationToken cancellationToken)
    {
        var column = await tasks.ListColumnForRebalanceAsync(columnId, cancellationToken);

        for (var index = 0; index < column.Count; index++)
        {
            column[index].Rebalance(index);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
