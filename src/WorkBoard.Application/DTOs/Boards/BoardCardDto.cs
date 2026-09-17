using WorkBoard.Application.DTOs.Common;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Boards;

/// <summary>
/// The card as rendered on the board.
/// </summary>
/// <remarks>
/// Deliberately narrow. A board can show several hundred cards at once, and the description alone is
/// up to 10 KB per task; including it would multiply the payload by an order of magnitude for text
/// no card ever displays. The full task is fetched only when a card is opened.
/// </remarks>
/// <param name="Id">Card identifier.</param>
/// <param name="Key">Human-readable key, such as <c>WB-42</c>.</param>
/// <param name="Title">Card title.</param>
/// <param name="Priority">Card priority.</param>
/// <param name="Position">Sparse ordering key within the column.</param>
/// <param name="DueAtUtc">Deadline, if set.</param>
/// <param name="IsOverdue">Computed server-side so every client agrees on "late".</param>
/// <param name="CommentCount">Comments that have not been deleted.</param>
/// <param name="SubtaskCount">Child cards.</param>
/// <param name="CompletedSubtaskCount">Child cards that have reached a done column.</param>
/// <param name="Assignees">Assigned members, initials only.</param>
/// <param name="Labels">Applied labels.</param>
/// <param name="RowVersion">Concurrency token, sent back when the card is dragged.</param>
public sealed record BoardCardDto(
    Guid Id,
    string Key,
    string Title,
    TaskPriority Priority,
    double Position,
    DateTimeOffset? DueAtUtc,
    bool IsOverdue,
    int CommentCount,
    int SubtaskCount,
    int CompletedSubtaskCount,
    IReadOnlyList<BoardAssigneeDto> Assignees,
    IReadOnlyList<LabelChipDto> Labels,
    byte[] RowVersion);
