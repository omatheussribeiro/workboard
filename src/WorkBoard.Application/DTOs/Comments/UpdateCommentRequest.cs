namespace WorkBoard.Application.DTOs.Comments;

/// <summary>An edit to an existing comment.</summary>
/// <param name="Body">Replacement text.</param>
public sealed record UpdateCommentRequest(string Body);
