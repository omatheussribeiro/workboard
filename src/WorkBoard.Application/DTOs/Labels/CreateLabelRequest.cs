namespace WorkBoard.Application.DTOs.Labels;

/// <summary>A new workspace label.</summary>
/// <param name="Name">Label text; unique within the workspace.</param>
/// <param name="ColorHex">Six hex digits, with or without a leading <c>#</c>.</param>
public sealed record CreateLabelRequest(string Name, string ColorHex);
