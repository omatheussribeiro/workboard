using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Events;
using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// A task: the unit everything else in the product revolves around.
/// </summary>
/// <remarks>
/// <para>
/// The CLR type is <c>WorkItem</c> while the domain concept, the table and the whole API surface
/// are called "task". A type named <c>Task</c> would collide with
/// <see cref="System.Threading.Tasks.Task"/> in every async method in the codebase, forcing alias
/// directives or fully-qualified names on hundreds of lines. The name is mapped back in exactly one
/// place — the EF configuration says <c>ToTable("Tasks")</c> — so the cost is a single line and no
/// file ever has to disambiguate. See ADR-007.
/// </para>
/// <para>
/// Subtasks are self-referencing through <see cref="ParentTaskId"/> and limited to one level:
/// arbitrary nesting makes rollups, board rendering and recursive queries far more expensive than
/// the feature is worth at this scale.
/// </para>
/// </remarks>
public sealed class WorkItem : Entity, IWorkspaceScoped, IAuditable
{
    /// <summary>Gap between adjacent tasks when a column is renumbered. See <see cref="Position"/>.</summary>
    private const double PositionGap = 1024d;

    private readonly List<WorkItem> _subtasks = [];
    private readonly List<TaskAssignment> _assignments = [];
    private readonly List<TaskLabel> _labels = [];
    private readonly List<Comment> _comments = [];

    private WorkItem() { }

    private WorkItem(Guid id, Guid workspaceId, Guid projectId, Guid boardColumnId, int number, string title, Guid createdById)
        : base(id)
    {
        WorkspaceId = workspaceId;
        ProjectId = projectId;
        BoardColumnId = boardColumnId;
        Number = number;
        Title = title;
        CreatedById = createdById;
    }

    public Guid WorkspaceId { get; private set; }

    /// <summary>A task always belongs to a project. There is no construction path that omits it.</summary>
    public Guid ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    public Guid BoardColumnId { get; private set; }

    public BoardColumn BoardColumn { get; private set; } = null!;

    /// <summary>Per-project sequence behind the human-readable key (WB-42). Allocated by the project.</summary>
    public int Number { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;

    /// <summary>
    /// Sort order inside the column, as a sparse double.
    /// </summary>
    /// <remarks>
    /// Dropping a card between two others sets the midpoint of its neighbours, so a reorder writes
    /// one row instead of renumbering the whole column — which matters because drag-and-drop fires a
    /// write per drop. The trade-off is that repeatedly halving the same gap eventually exhausts
    /// double precision; <see cref="NeedsRebalance"/> flags that so the application can renumber the
    /// column. In practice it is rare, and a rare O(n) rebalance beats a constant O(n) write.
    /// </remarks>
    public double Position { get; private set; }

    public DateTimeOffset? DueAtUtc { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guid? ParentTaskId { get; private set; }

    public WorkItem? ParentTask { get; private set; }

    public Guid CreatedById { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// SQL Server rowversion. A board is a shared surface: two people dragging the same card, or one
    /// editing while another moves it, is routine rather than exceptional. The token turns that into
    /// a 409 the client can resolve instead of a silent last-write-wins. See ADR-008.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<WorkItem> Subtasks => _subtasks;

    public IReadOnlyCollection<TaskAssignment> Assignments => _assignments;

    public IReadOnlyCollection<TaskLabel> Labels => _labels;

    public IReadOnlyCollection<Comment> Comments => _comments;

    public bool IsSubtask => ParentTaskId is not null;

    /// <summary>True once <see cref="Position"/> gaps have shrunk far enough to risk collisions.</summary>
    public bool NeedsRebalance { get; private set; }

    public bool IsOverdue(DateTimeOffset nowUtc) =>
        DueAtUtc is { } due && CompletedAtUtc is null && due < nowUtc;

    internal static WorkItem Create(Guid workspaceId, Guid projectId, Guid boardColumnId, int number, string title, Guid createdById)
    {
        var task = new WorkItem(
            Guid.CreateVersion7(),
            Guard.NotDefault(workspaceId),
            Guard.NotDefault(projectId),
            Guard.NotDefault(boardColumnId),
            number,
            Guard.MaxLength(Guard.NotEmpty(title), 200),
            Guard.NotDefault(createdById));

        task.Raise(new TaskCreated(task.Id, workspaceId, projectId, task.Title, createdById));
        return task;
    }

    public void ChangeTitle(string title, Guid actorId)
    {
        var next = Guard.MaxLength(Guard.NotEmpty(title), 200);
        if (next == Title)
        {
            return;
        }

        var previous = Title;
        Title = next;
        Raise(new TaskFieldChanged(Id, WorkspaceId, nameof(Title), previous, next, actorId));
    }

    public void ChangeDescription(string? description) =>
        Description = description is null ? null : Guard.MaxLength(description, 10_000);

    public void ChangePriority(TaskPriority priority, Guid actorId)
    {
        if (priority == Priority)
        {
            return;
        }

        var previous = Priority;
        Priority = priority;
        Raise(new TaskFieldChanged(Id, WorkspaceId, nameof(Priority), previous.ToString(), priority.ToString(), actorId));
    }

    /// <summary>
    /// Sets or clears the deadline.
    /// </summary>
    /// <remarks>
    /// A deadline in the past is rejected, but only when it is being set or moved — an existing
    /// deadline that has simply elapsed stays valid, otherwise every overdue task would become
    /// unsaveable. A subtask may not outlive its parent deadline.
    /// </remarks>
    public void SetDeadline(DateTimeOffset? dueAtUtc, DateTimeOffset nowUtc, Guid actorId)
    {
        if (dueAtUtc == DueAtUtc)
        {
            return;
        }

        if (dueAtUtc is { } due)
        {
            Guard.Against(due <= nowUtc, "Deadline must be in the future.");
            Guard.Against(
                ParentTask is { DueAtUtc: { } parentDue } && due > parentDue,
                "A subtask cannot be due after its parent task.");
        }

        var previous = DueAtUtc;
        DueAtUtc = dueAtUtc;
        Raise(new TaskFieldChanged(Id, WorkspaceId, nameof(DueAtUtc), Format(previous), Format(dueAtUtc), actorId));
    }

    /// <summary>
    /// Moves the task within or between columns.
    /// </summary>
    /// <remarks>
    /// Both columns are passed in so the aggregate can maintain the WIP counters and enforce the
    /// target limit without the caller reaching into either one. The status timestamps
    /// (<see cref="StartedAtUtc"/>, <see cref="CompletedAtUtc"/>) are derived from the target column
    /// category, which is why the category lives on the column: cycle-time reporting stays correct
    /// even when a team renames or inserts columns.
    /// </remarks>
    public void MoveTo(
        BoardColumn target,
        BoardColumn current,
        double? previousPosition,
        double? nextPosition,
        DateTimeOffset nowUtc,
        Guid actorId)
    {
        Guard.Against(target.WorkspaceId != WorkspaceId, "Cannot move a task into another workspace.");
        Guard.Against(current.Id != BoardColumnId, "The supplied current column does not match the task.");

        var changedColumn = target.Id != BoardColumnId;
        if (changedColumn)
        {
            target.OnTaskEntered();
            current.OnTaskLeft();
            BoardColumnId = target.Id;
        }

        Position = ComputePosition(previousPosition, nextPosition);
        ApplyStatusTimestamps(target.Category, nowUtc);

        if (changedColumn)
        {
            Raise(new TaskMoved(Id, WorkspaceId, ProjectId, current.Id, target.Id, target.Category, actorId));
        }
    }

    /// <summary>Places a freshly created task at the end of its column.</summary>
    public void PlaceAtEndOf(BoardColumn column, double? lastPosition)
    {
        Guard.Against(column.Id != BoardColumnId, "Column does not match the task.");

        column.OnTaskEntered();
        Position = (lastPosition ?? 0d) + PositionGap;
    }

    /// <summary>Applied after the application renumbers a column that hit <see cref="NeedsRebalance"/>.</summary>
    public void Rebalance(int index)
    {
        Position = (index + 1) * PositionGap;
        NeedsRebalance = false;
    }

    public TaskAssignment Assign(Guid userId, Guid actorId)
    {
        Guard.Against(_assignments.Any(a => a.UserId == userId), "User is already assigned to this task.");

        var assignment = TaskAssignment.Create(WorkspaceId, Id, userId, actorId);
        _assignments.Add(assignment);
        Raise(new TaskAssigned(Id, WorkspaceId, ProjectId, userId, actorId, Title));
        return assignment;
    }

    public void Unassign(Guid userId)
    {
        var assignment = _assignments.SingleOrDefault(a => a.UserId == userId);
        if (assignment is null)
        {
            return;
        }

        _assignments.Remove(assignment);
        Raise(new TaskUnassigned(Id, WorkspaceId, userId));
    }

    public void ApplyLabel(Label label)
    {
        Guard.Against(label.WorkspaceId != WorkspaceId, "Label belongs to a different workspace.");

        if (_labels.Any(l => l.LabelId == label.Id))
        {
            return;
        }

        _labels.Add(TaskLabel.Create(WorkspaceId, Id, label.Id));
    }

    public void RemoveLabel(Guid labelId)
    {
        var link = _labels.SingleOrDefault(l => l.LabelId == labelId);
        if (link is not null)
        {
            _labels.Remove(link);
        }
    }

    public Comment AddComment(string body, Guid authorId, DateTimeOffset nowUtc)
    {
        var comment = Comment.Create(WorkspaceId, Id, authorId, body, nowUtc);
        _comments.Add(comment);
        Raise(new TaskCommented(Id, WorkspaceId, ProjectId, comment.Id, authorId, Title));
        return comment;
    }

    /// <summary>Attaches an existing task in the same project as a subtask of this one.</summary>
    public void AddSubtask(WorkItem subtask)
    {
        Guard.Against(subtask.Id == Id, "A task cannot be its own subtask.");
        Guard.Against(subtask.ProjectId != ProjectId, "A subtask must live in the same project as its parent.");
        Guard.Against(IsSubtask, "Subtasks cannot be nested more than one level deep.");
        Guard.Against(subtask._subtasks.Count > 0, "A task that already has subtasks cannot become one.");

        subtask.ParentTaskId = Id;
        subtask.ParentTask = this;
        _subtasks.Add(subtask);
    }

    public void DetachFromParent()
    {
        ParentTaskId = null;
        ParentTask = null;
    }

    void IAuditable.SetCreated(DateTimeOffset at) => CreatedAtUtc = at;

    void IAuditable.SetUpdated(DateTimeOffset at) => UpdatedAtUtc = at;

    private void ApplyStatusTimestamps(TaskStatusCategory category, DateTimeOffset nowUtc)
    {
        switch (category)
        {
            case TaskStatusCategory.Todo:
                StartedAtUtc = null;
                CompletedAtUtc = null;
                break;

            case TaskStatusCategory.InProgress:
                StartedAtUtc ??= nowUtc;
                CompletedAtUtc = null;
                break;

            case TaskStatusCategory.Done:
                // A card dragged straight from To Do to Done never "started"; record it anyway so
                // cycle-time reporting has a non-null interval instead of silently dropping the task.
                StartedAtUtc ??= nowUtc;
                CompletedAtUtc = nowUtc;
                break;

            default:
                throw new DomainException($"Unknown status category {category}.");
        }
    }

    private double ComputePosition(double? previous, double? next)
    {
        var position = (previous, next) switch
        {
            (null, null) => PositionGap,
            (null, { } n) => n - PositionGap,
            ({ } p, null) => p + PositionGap,
            ({ } p, { } n) => (p + n) / 2d,
        };

        // Once neighbours are this close, further midpoints stop being usefully representable.
        NeedsRebalance = previous is { } lo && next is { } hi && Math.Abs(hi - lo) < 0.001d;
        return position;
    }

    private static string? Format(DateTimeOffset? value) => value?.ToString("O");
}
