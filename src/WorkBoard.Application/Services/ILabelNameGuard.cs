namespace WorkBoard.Application.Services;

/// <summary>
/// Enforces that a label name is unique inside the workspace.
/// </summary>
/// <remarks>
/// A service rather than a private method, because two use cases need the identical rule — creating
/// a label and renaming one — and the version that lived only in the create path is exactly why a
/// rename onto an existing name used to surface as a 500.
/// </remarks>
public interface ILabelNameGuard
{
    /// <summary>Throws <c>ConflictException</c> when the name is already used.</summary>
    /// <param name="name">Trimmed label name.</param>
    /// <param name="exceptId">The label being renamed, excluded from its own check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureFreeAsync(string name, Guid? exceptId, CancellationToken cancellationToken);
}
