namespace WorkBoard.Application.Exceptions;

/// <summary>Authenticated, but not permitted.</summary>
public sealed class ForbiddenException(string message) : WorkBoardException(message);
