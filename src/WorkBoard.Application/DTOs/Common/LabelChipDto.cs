namespace WorkBoard.Application.DTOs.Common;

/// <summary>A label as a coloured chip. Identical wherever a label is rendered, so defined once.</summary>
/// <param name="Id">Label identifier.</param>
/// <param name="Name">Chip text.</param>
/// <param name="ColorHex">Chip colour, normalised to <c>#RRGGBB</c>.</param>
public sealed record LabelChipDto(Guid Id, string Name, string ColorHex);
