namespace WorkBoard.Application.DTOs.Comments;

/// <summary>A new comment on a task.</summary>
/// <param name="Body">Comment text, up to 5,000 characters.</param>
public sealed record CreateCommentRequest(string Body);
