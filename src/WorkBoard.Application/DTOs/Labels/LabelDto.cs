namespace WorkBoard.Application.DTOs.Labels;

/// <summary>A label on the settings page, with the number of cards carrying it.</summary>
/// <param name="Id">Label identifier.</param>
/// <param name="Name">Label text.</param>
/// <param name="ColorHex">Label colour.</param>
/// <param name="TaskCount">How many cards use it — shown before a delete, which untags them all.</param>
public sealed record LabelDto(Guid Id, string Name, string ColorHex, int TaskCount);
