namespace WorkBoard.Application.DTOs.Labels;

/// <summary>The editable fields of a label.</summary>
/// <param name="Name">New text.</param>
/// <param name="ColorHex">New colour.</param>
public sealed record UpdateLabelRequest(string Name, string ColorHex);
