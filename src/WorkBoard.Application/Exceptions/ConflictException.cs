namespace WorkBoard.Application.Exceptions;

/// <summary>The request conflicts with current state — a duplicate key, or a stale concurrency token.</summary>
public sealed class ConflictException(string message) : WorkBoardException(message);
