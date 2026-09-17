namespace WorkBoard.Application.DTOs.Search;

/// <summary>One row of the command palette, whatever kind of thing it points at.</summary>
/// <param name="Id">Identifier of the matched entity.</param>
/// <param name="Title">Primary line.</param>
/// <param name="Subtitle">Secondary line: a card key, a project key, or a snippet.</param>
/// <param name="TaskId">Card to navigate to, when the hit resolves to one.</param>
/// <param name="ProjectId">Project to navigate to, when the hit resolves to one.</param>
public sealed record SearchHitDto(Guid Id, string Title, string? Subtitle, Guid? TaskId, Guid? ProjectId);
