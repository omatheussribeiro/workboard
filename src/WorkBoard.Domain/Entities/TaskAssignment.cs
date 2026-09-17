using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// Links a task to one of its assignees.
/// </summary>
/// <remarks>
/// A many-to-many with a payload (who assigned, when), so it is an explicit entity with the
/// composite key <c>(TaskId, UserId)</c> rather than an EF skip navigation. The composite key is
/// itself the uniqueness rule: the same user cannot be assigned twice.
/// </remarks>
public sealed class TaskAssignment : IWorkspaceScoped
{
    private TaskAssignment() { }

    private TaskAssignment(Guid workspaceId, Guid taskId, Guid userId, Guid assignedById)
    {
        WorkspaceId = workspaceId;
        TaskId = taskId;
        UserId = userId;
        AssignedById = assignedById;
        AssignedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid WorkspaceId { get; private set; }

    public Guid TaskId { get; private set; }

    public WorkItem Task { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public Guid AssignedById { get; private set; }

    public DateTimeOffset AssignedAtUtc { get; private set; }

    internal static TaskAssignment Create(Guid workspaceId, Guid taskId, Guid userId, Guid assignedById) =>
        new(Guard.NotDefault(workspaceId), Guard.NotDefault(taskId), Guard.NotDefault(userId), Guard.NotDefault(assignedById));
}
