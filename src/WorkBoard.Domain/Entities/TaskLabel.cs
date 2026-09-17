using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>Join row between a task and a workspace label.</summary>
public sealed class TaskLabel : IWorkspaceScoped
{
    private TaskLabel() { }

    private TaskLabel(Guid workspaceId, Guid taskId, Guid labelId)
    {
        WorkspaceId = workspaceId;
        TaskId = taskId;
        LabelId = labelId;
        AppliedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid WorkspaceId { get; private set; }

    public Guid TaskId { get; private set; }

    public WorkItem Task { get; private set; } = null!;

    public Guid LabelId { get; private set; }

    public Label Label { get; private set; } = null!;

    public DateTimeOffset AppliedAtUtc { get; private set; }

    internal static TaskLabel Create(Guid workspaceId, Guid taskId, Guid labelId) =>
        new(Guard.NotDefault(workspaceId), Guard.NotDefault(taskId), Guard.NotDefault(labelId));
}
