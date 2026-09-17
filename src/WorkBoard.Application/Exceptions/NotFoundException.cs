namespace WorkBoard.Application.Exceptions;

/// <summary>
/// The entity does not exist, or is invisible to this tenant — the API cannot tell them apart, by design.
/// </summary>
/// <remarks>
/// Collapsing "absent" and "belongs to someone else" into one response is deliberate. Distinguishing
/// them turns the API into an oracle for which ids exist in other workspaces, which is an
/// enumeration channel even though no data is returned.
/// </remarks>
public sealed class NotFoundException(string resource, object key)
    : WorkBoardException($"{resource} '{key}' was not found.")
{
    /// <summary>The kind of entity that was not found, used to build the ProblemDetails title.</summary>
    public string Resource { get; } = resource;
}
