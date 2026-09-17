namespace WorkBoard.Application.Interfaces;

/// <summary>
/// Commits a unit of work and owns the translation of storage-level conflicts.
/// </summary>
/// <remarks>
/// <para>
/// The repositories in this namespace never save. They stage changes; a service decides when the
/// work is complete and commits once. That is what keeps a command such as "create a task" — which
/// bumps the project counter, inserts the task, its assignments, its labels, and the activity rows
/// the domain-event dispatcher produces — inside a single transaction rather than five.
/// </para>
/// <para>
/// <see cref="SaveChangesAsync"/> lets the provider exception escape;
/// <see cref="SaveChangesWithConcurrencyCheckAsync"/> converts the optimistic-concurrency failure
/// into a <c>ConflictException</c> carrying a message the user can act on. Both exist because only
/// the caller knows whether a stale row is an expected outcome (a drag-and-drop race) or a bug.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Commits the staged changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits, translating an optimistic-concurrency failure into a conflict the API returns as 409.
    /// </summary>
    /// <param name="conflictMessage">Message shown to the user when the row changed underneath them.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesWithConcurrencyCheckAsync(string conflictMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a transaction, through the provider's execution strategy.
    /// </summary>
    /// <remarks>
    /// Going through the execution strategy is not optional once retries are enabled: EF refuses an
    /// explicitly opened transaction otherwise, because it cannot replay a block of user code it
    /// did not author. The strategy re-invokes the whole delegate, so the delegate must be safe to
    /// run more than once.
    /// </remarks>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
