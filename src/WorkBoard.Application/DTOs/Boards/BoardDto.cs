namespace WorkBoard.Application.DTOs.Boards;

/// <summary>A whole Kanban board, columns and cards included.</summary>
/// <param name="Id">Board identifier.</param>
/// <param name="Name">Board name.</param>
/// <param name="ProjectId">Owning project.</param>
/// <param name="ProjectKey">Owning project key, used to render card keys.</param>
/// <param name="Columns">Columns in display order.</param>
public sealed record BoardDto(
    Guid Id,
    string Name,
    Guid ProjectId,
    string ProjectKey,
    IReadOnlyList<BoardColumnDto> Columns);
