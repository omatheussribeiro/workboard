namespace WorkBoard.Application.DTOs.Comments;

/// <summary>One comment in a task thread.</summary>
/// <param name="Id">Comment identifier.</param>
/// <param name="TaskId">Task the comment belongs to.</param>
/// <param name="AuthorId">Author identifier.</param>
/// <param name="AuthorName">Author display name.</param>
/// <param name="Body">Comment text.</param>
/// <param name="CreatedAtUtc">When it was posted.</param>
/// <param name="EditedAtUtc">When it was last edited, if ever.</param>
/// <param name="CanEdit">
/// Whether the calling user may edit this one. Computed server-side so the client does not have to
/// re-derive the rule — and so a client that gets it wrong still cannot act on it, because the
/// entity enforces the same rule on write.
/// </param>
public sealed record CommentDto(
    Guid Id,
    Guid TaskId,
    Guid AuthorId,
    string AuthorName,
    string Body,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? EditedAtUtc,
    bool CanEdit);
