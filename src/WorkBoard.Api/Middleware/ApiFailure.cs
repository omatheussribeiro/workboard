namespace WorkBoard.Api.Middleware;

/// <summary>
/// The wire shape of a failure, before it becomes <c>ProblemDetails</c>.
/// </summary>
/// <remarks>
/// A named record rather than the four-element tuple this used to be. The tuple read as
/// <c>(status, title, detail, errors)</c> at the declaration and as <c>Item3</c> everywhere the
/// compiler could not carry the names, and adding a fifth element would have been a silent
/// positional change at every one of the ten construction sites.
/// </remarks>
/// <param name="Status">HTTP status code.</param>
/// <param name="Title">Short, stable summary of the problem type.</param>
/// <param name="Detail">Human-readable explanation of this occurrence.</param>
/// <param name="Errors">Per-field validation messages, when the failure is a validation failure.</param>
internal sealed record ApiFailure(
    int Status,
    string Title,
    string Detail,
    IDictionary<string, string[]>? Errors = null);
