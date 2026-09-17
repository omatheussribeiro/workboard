using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// One lane of a board. Holds the ordered tasks currently in that state.
/// </summary>
public sealed class BoardColumn : Entity, IWorkspaceScoped
{
    private readonly List<WorkItem> _tasks = [];

    private BoardColumn() { }

    private BoardColumn(Guid id, Guid workspaceId, Guid boardId, string name, int position, TaskStatusCategory category, int? wipLimit)
        : base(id)
    {
        WorkspaceId = workspaceId;
        BoardId = boardId;
        Name = name;
        Position = position;
        Category = category;
        WipLimit = wipLimit;
    }

    public Guid WorkspaceId { get; private set; }

    public Guid BoardId { get; private set; }

    public Board Board { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Dense 0-based display order within the board.</summary>
    public int Position { get; private set; }

    public TaskStatusCategory Category { get; private set; }

    /// <summary>Optional work-in-progress limit. Null means unlimited.</summary>
    public int? WipLimit { get; private set; }

    /// <summary>
    /// Maintained in the domain so the WIP check needs no extra query.
    /// The persistence layer keeps it in step by routing every move through the aggregate.
    /// </summary>
    public int TaskCount { get; private set; }

    public IReadOnlyCollection<WorkItem> Tasks => _tasks;

    internal static BoardColumn Create(Guid workspaceId, Guid boardId, string name, int position, TaskStatusCategory category, int? wipLimit)
    {
        Guard.Against(wipLimit is <= 0, "WIP limit must be greater than zero when set.");

        return new BoardColumn(
            Guid.CreateVersion7(),
            Guard.NotDefault(workspaceId),
            Guard.NotDefault(boardId),
            Guard.MaxLength(Guard.NotEmpty(name), 60),
            position,
            category,
            wipLimit);
    }

    public void Rename(string name) => Name = Guard.MaxLength(Guard.NotEmpty(name), 60);

    public void SetWipLimit(int? wipLimit)
    {
        Guard.Against(wipLimit is <= 0, "WIP limit must be greater than zero when set.");
        WipLimit = wipLimit;
    }

    public void ChangeCategory(TaskStatusCategory category) => Category = category;

    internal void SetPosition(int position) => Position = position;

    internal void OnTaskEntered()
    {
        Guard.Against(
            WipLimit is not null && TaskCount >= WipLimit,
            $"Column '{Name}' has reached its WIP limit of {WipLimit}.");

        TaskCount++;
    }

    internal void OnTaskLeft() => TaskCount = Math.Max(0, TaskCount - 1);
}
