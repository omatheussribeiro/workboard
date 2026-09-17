namespace WorkBoard.Application.Exceptions;

/// <summary>
/// Conflict messages shown to more than one caller.
/// </summary>
/// <remarks>
/// These are user-facing strings, which makes them part of the contract: two use cases reporting the
/// same conflict in two slightly different sentences is a UI bug that nothing would catch.
/// </remarks>
public static class ConflictMessages
{
    /// <summary>Returned when a write loses the optimistic-concurrency check on a task.</summary>
    public const string StaleTask = "This task was changed by someone else. Reload and try again.";
}
