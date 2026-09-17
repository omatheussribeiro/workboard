using WorkBoard.Domain.Events;
using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// A unit of work inside a workspace. Owns its boards and its tasks.
/// </summary>
/// <remarks>
/// The project is the consistency boundary for task numbering: <see cref="NextTaskNumber"/> hands
/// out the human-readable sequence (WB-1, WB-2, ...). That counter lives on the row and is
/// protected by optimistic concurrency, so two concurrent task creations cannot both claim the same
/// number — one retries. Using a database IDENTITY would be simpler but global, not per project.
/// </remarks>
public sealed class Project : Entity, IWorkspaceScoped, IAuditable
{
    private readonly List<Board> _boards = [];
    private readonly List<WorkItem> _tasks = [];

    private Project() { }

    private Project(Guid id, Guid workspaceId, string name, string key, Guid createdById)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Key = key;
        CreatedById = createdById;
    }

    public Guid WorkspaceId { get; private set; }

    public Workspace Workspace { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Short uppercase prefix for task keys, unique within the workspace.</summary>
    public string Key { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid CreatedById { get; private set; }

    public bool IsArchived { get; private set; }

    /// <summary>Last handed-out task number. Guarded by <see cref="RowVersion"/>.</summary>
    public int TaskCounter { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    /// <summary>SQL Server rowversion. Present so concurrent task creation cannot duplicate a number.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<Board> Boards => _boards;

    public IReadOnlyCollection<WorkItem> Tasks => _tasks;

    public static Project Create(Guid workspaceId, string name, string key, Guid createdById)
    {
        var project = new Project(
            Guid.CreateVersion7(),
            Guard.NotDefault(workspaceId),
            Guard.MaxLength(Guard.NotEmpty(name), 120),
            NormalizeKey(key),
            Guard.NotDefault(createdById));

        project.Raise(new ProjectCreated(project.Id, workspaceId, project.Name, createdById));
        return project;
    }

    /// <summary>
    /// Creates the project together with the default Kanban board, so a newly created project is
    /// immediately usable instead of landing the user on an empty screen.
    /// </summary>
    public static Project CreateWithDefaultBoard(Guid workspaceId, string name, string key, Guid createdById)
    {
        var project = Create(workspaceId, name, key, createdById);
        project.AddBoard(Board.CreateDefault(project.WorkspaceId, project.Id, "Board"));
        return project;
    }

    public void Rename(string name) => Name = Guard.MaxLength(Guard.NotEmpty(name), 120);

    public void Describe(string? description) =>
        Description = description is null ? null : Guard.MaxLength(description, 2000);

    public void Archive()
    {
        IsArchived = true;
        Raise(new ProjectArchived(Id, WorkspaceId));
    }

    public void Restore() => IsArchived = false;

    public Board AddBoard(Board board)
    {
        Guard.Against(board.ProjectId != Id, "Board belongs to a different project.");
        Guard.Against(board.WorkspaceId != WorkspaceId, "Board belongs to a different workspace.");

        _boards.Add(board);
        return board;
    }

    /// <summary>
    /// The only sanctioned way to create a task: it forces the task into this project, inherits the
    /// workspace, and allocates the next number under the same concurrency token.
    /// </summary>
    public WorkItem CreateTask(Guid boardColumnId, string title, Guid createdById)
    {
        Guard.Against(IsArchived, "Cannot add tasks to an archived project.");

        var task = WorkItem.Create(WorkspaceId, Id, boardColumnId, NextTaskNumber(), title, createdById);
        _tasks.Add(task);
        return task;
    }

    public string FormatTaskKey(int number) => $"{Key}-{number}";

    void IAuditable.SetCreated(DateTimeOffset at) => CreatedAtUtc = at;

    void IAuditable.SetUpdated(DateTimeOffset at) => UpdatedAtUtc = at;

    private int NextTaskNumber() => ++TaskCounter;

    private static string NormalizeKey(string key)
    {
        var value = Guard.MaxLength(Guard.NotEmpty(key), 10).ToUpperInvariant();
        Guard.Against(value.Length < 2, "Project key must be at least 2 characters.");
        Guard.Against(!value.All(char.IsAsciiLetterUpper), "Project key may only contain letters.");
        return value;
    }
}
