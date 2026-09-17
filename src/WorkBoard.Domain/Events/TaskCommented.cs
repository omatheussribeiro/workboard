using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when a comment is posted. Notifies the card's assignees.</summary>
/// <param name="TaskId">The card.</param>
/// <param name="WorkspaceId">Owning workspace.</param>
/// <param name="ProjectId">Owning project.</param>
/// <param name="CommentId">The new comment.</param>
/// <param name="AuthorId">Who wrote it.</param>
/// <param name="Title">Card title, so the notification can be written without a second query.</param>
public sealed record TaskCommented(
    Guid TaskId,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid CommentId,
    Guid AuthorId,
    string Title) : IDomainEvent;
