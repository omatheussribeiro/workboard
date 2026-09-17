using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// A comment on a task. Soft-deleted rather than removed, so a thread keeps its shape and the
/// activity feed does not end up referencing rows that no longer exist.
/// </summary>
public sealed class Comment : Entity, IWorkspaceScoped
{
    private Comment() { }

    private Comment(Guid id, Guid workspaceId, Guid taskId, Guid authorId, string body, DateTimeOffset createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        TaskId = taskId;
        AuthorId = authorId;
        Body = body;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid WorkspaceId { get; private set; }

    public Guid TaskId { get; private set; }

    public WorkItem Task { get; private set; } = null!;

    public Guid AuthorId { get; private set; }

    public User Author { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? EditedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    internal static Comment Create(Guid workspaceId, Guid taskId, Guid authorId, string body, DateTimeOffset createdAtUtc) =>
        new(Guid.CreateVersion7(),
            Guard.NotDefault(workspaceId),
            Guard.NotDefault(taskId),
            Guard.NotDefault(authorId),
            Guard.MaxLength(Guard.NotEmpty(body), 5000),
            createdAtUtc);

    /// <summary>Only the author may edit; enforced here so no endpoint can forget it.</summary>
    public void Edit(string body, Guid actorId, DateTimeOffset nowUtc)
    {
        Guard.Against(actorId != AuthorId, "Only the author can edit a comment.");
        Guard.Against(IsDeleted, "A deleted comment cannot be edited.");

        Body = Guard.MaxLength(Guard.NotEmpty(body), 5000);
        EditedAtUtc = nowUtc;
    }

    /// <summary>The author or a workspace admin may delete. Admin rights are resolved by the caller.</summary>
    public void Delete(Guid actorId, bool actorIsWorkspaceAdmin)
    {
        Guard.Against(actorId != AuthorId && !actorIsWorkspaceAdmin, "Only the author or a workspace admin can delete a comment.");

        IsDeleted = true;
        Body = string.Empty;
    }
}
