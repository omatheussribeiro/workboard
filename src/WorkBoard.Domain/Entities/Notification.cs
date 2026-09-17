using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// An in-app notification addressed to one user.
/// </summary>
/// <remarks>
/// Workspace-scoped as well as user-scoped: leaving a workspace should stop surfacing its
/// notifications, and the tenant filter makes that automatic rather than a rule to remember.
/// </remarks>
public sealed class Notification : Entity, IWorkspaceScoped
{
    private Notification() { }

    private Notification(Guid id, Guid workspaceId, Guid recipientId, NotificationType type, string message, DateTimeOffset createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        RecipientId = recipientId;
        Type = type;
        Message = message;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid WorkspaceId { get; private set; }

    public Guid RecipientId { get; private set; }

    public User Recipient { get; private set; } = null!;

    public NotificationType Type { get; private set; }

    public string Message { get; private set; } = null!;

    public Guid? TaskId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    public static Notification Create(
        Guid workspaceId,
        Guid recipientId,
        NotificationType type,
        string message,
        DateTimeOffset createdAtUtc,
        Guid? taskId = null) =>
        new(Guid.CreateVersion7(),
            Guard.NotDefault(workspaceId),
            Guard.NotDefault(recipientId),
            type,
            Guard.MaxLength(Guard.NotEmpty(message), 500),
            createdAtUtc)
        {
            TaskId = taskId,
        };

    public void MarkRead(DateTimeOffset nowUtc)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = nowUtc;
    }
}
