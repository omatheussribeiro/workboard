namespace WorkBoard.Application.DTOs.Boards;

/// <summary>Reorders a column.</summary>
/// <param name="Position">Zero-based target index; the aggregate re-densifies the rest.</param>
public sealed record MoveColumnRequest(int Position);
