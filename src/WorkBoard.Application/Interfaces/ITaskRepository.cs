using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>
/// Write-side access to the task aggregate.
/// </summary>
/// <remarks>
/// <para>
/// This is not a generic repository and there is no <c>IRepository&lt;T&gt;</c> to derive it from.
/// Every method here exists because loading a task <em>for modification</em> has a correct shape —
/// the assignments, labels and parent have to be tracked, or the aggregate's own invariants run
/// against half-loaded collections — and that shape was previously copied into each service that
/// needed it. Naming it once removes the copy and makes "forgot an Include" impossible to reach by
/// accident. See ADR-013.
/// </para>
/// <para>
/// Reads are deliberately absent. Listing, searching and projecting stay on
/// <see cref="IWorkBoardDbContext"/> inside the query services, where LINQ composes into a single
/// SQL statement. Routing them through methods here would return entities that then need mapping in
/// memory, which is the exact cost the projections were written to avoid.
/// </para>
/// </remarks>
public interface ITaskRepository
{
    /// <summary>Loads a task tracked, with no related collections, for operations that only need the root.</summary>
    Task<WorkItem?> FindAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>Loads a task tracked, with the collections its own methods mutate.</summary>
    /// <returns>The task, or <c>null</c> when it does not exist in the current workspace.</returns>
    Task<WorkItem?> FindForUpdateAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>Loads a task tracked together with its subtasks, for deletion and re-parenting.</summary>
    Task<WorkItem?> FindWithSubtasksAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>Tells EF the concurrency token the caller last saw, so the UPDATE carries it in its WHERE clause.</summary>
    void SetOriginalRowVersion(WorkItem task, byte[] rowVersion);

    /// <summary>The highest position in a column, or <c>null</c> when the column is empty.</summary>
    Task<double?> GetLastPositionAsync(Guid columnId, CancellationToken cancellationToken);

    /// <summary>
    /// Positions of the cards a drop landed between, read in one query.
    /// </summary>
    /// <remarks>
    /// Ids absent from <paramref name="columnId"/> are simply missing from the result, which is what
    /// makes a stale neighbour id degrade into "append" rather than into an error.
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, double>> GetPositionsAsync(
        Guid columnId,
        IReadOnlyCollection<Guid> taskIds,
        CancellationToken cancellationToken);

    /// <summary>Every task in a column, tracked and ordered by position, for a rebalance.</summary>
    Task<IReadOnlyList<WorkItem>> ListColumnForRebalanceAsync(Guid columnId, CancellationToken cancellationToken);

    /// <summary>Stages a new task.</summary>
    void Add(WorkItem task);

    /// <summary>Stages a deletion.</summary>
    void Remove(WorkItem task);
}
