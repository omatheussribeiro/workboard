using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Boards;

/// <summary>One column of a board.</summary>
/// <param name="Id">Column identifier.</param>
/// <param name="Name">Column heading.</param>
/// <param name="Position">Zero-based display order.</param>
/// <param name="Category">Status category cards in this column are considered to be in.</param>
/// <param name="WipLimit">Optional work-in-progress limit.</param>
/// <param name="TaskCount">Top-level cards in the column.</param>
/// <param name="Cards">Cards in drop order.</param>
public sealed record BoardColumnDto(
    Guid Id,
    string Name,
    int Position,
    TaskStatusCategory Category,
    int? WipLimit,
    int TaskCount,
    IReadOnlyList<BoardCardDto> Cards);
