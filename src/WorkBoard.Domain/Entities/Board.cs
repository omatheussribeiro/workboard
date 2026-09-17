using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// A Kanban view over a project: an ordered set of columns that tasks move between.
/// </summary>
public sealed class Board : Entity, IWorkspaceScoped, IAuditable
{
    private readonly List<BoardColumn> _columns = [];

    private Board() { }

    private Board(Guid id, Guid workspaceId, Guid projectId, string name)
        : base(id)
    {
        WorkspaceId = workspaceId;
        ProjectId = projectId;
        Name = name;
    }

    public Guid WorkspaceId { get; private set; }

    public Guid ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// The columns of this board, unordered.
    /// </summary>
    /// <remarks>
    /// Exposes the backing collection as-is rather than returning <c>_columns.OrderBy(...)</c>.
    /// A property that sorts would be convenient in memory but untranslatable in a LINQ projection:
    /// EF Core can map a navigation to a backing field, not evaluate arbitrary code inside one. So
    /// ordering by <see cref="BoardColumn.Position"/> is applied at each query instead.
    /// </remarks>
    public IReadOnlyCollection<BoardColumn> Columns => _columns;

    public static Board Create(Guid workspaceId, Guid projectId, string name) =>
        new(Guid.CreateVersion7(),
            Guard.NotDefault(workspaceId),
            Guard.NotDefault(projectId),
            Guard.MaxLength(Guard.NotEmpty(name), 120));

    /// <summary>A board seeded with the three columns every team starts from.</summary>
    public static Board CreateDefault(Guid workspaceId, Guid projectId, string name)
    {
        var board = Create(workspaceId, projectId, name);
        board.AddColumn("To Do", TaskStatusCategory.Todo);
        board.AddColumn("In Progress", TaskStatusCategory.InProgress);
        board.AddColumn("Done", TaskStatusCategory.Done);
        return board;
    }

    public void Rename(string name) => Name = Guard.MaxLength(Guard.NotEmpty(name), 120);

    public BoardColumn AddColumn(string name, TaskStatusCategory category, int? wipLimit = null)
    {
        Guard.Against(
            _columns.Any(c => string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)),
            "A column with that name already exists on this board.");

        var position = _columns.Count == 0 ? 0 : _columns.Max(c => c.Position) + 1;
        var column = BoardColumn.Create(WorkspaceId, Id, name, position, category, wipLimit);
        _columns.Add(column);
        return column;
    }

    public void RemoveColumn(Guid columnId)
    {
        var column = RequireColumn(columnId);
        Guard.Against(_columns.Count <= 1, "A board must keep at least one column.");
        Guard.Against(column.TaskCount > 0, "Move or delete the tasks in this column before removing it.");

        _columns.Remove(column);
        Renumber();
    }

    /// <summary>
    /// Moves a column to <paramref name="targetPosition"/> and renumbers the rest.
    /// </summary>
    /// <remarks>
    /// Positions are rewritten as a dense 0..n-1 sequence rather than using gap-based ordering.
    /// A board has single-digit column counts, so the write amplification is irrelevant and dense
    /// integers stay readable in the database, which gap schemes do not.
    /// </remarks>
    public void MoveColumn(Guid columnId, int targetPosition)
    {
        var column = RequireColumn(columnId);
        var ordered = _columns.OrderBy(c => c.Position).ToList();

        ordered.Remove(column);
        ordered.Insert(Math.Clamp(targetPosition, 0, ordered.Count), column);

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].SetPosition(i);
        }
    }

    public BoardColumn RequireColumn(Guid columnId) =>
        _columns.SingleOrDefault(c => c.Id == columnId)
        ?? throw new DomainException("Column does not belong to this board.");

    void IAuditable.SetCreated(DateTimeOffset at) => CreatedAtUtc = at;

    void IAuditable.SetUpdated(DateTimeOffset at) => UpdatedAtUtc = at;

    private void Renumber()
    {
        var ordered = _columns.OrderBy(c => c.Position).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].SetPosition(i);
        }
    }
}
