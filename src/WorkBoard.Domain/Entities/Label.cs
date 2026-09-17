using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// A named, coloured tag. Owned by the workspace rather than by a project, so the same taxonomy
/// (bug, tech-debt, customer-request) can be reused and filtered across every project in the tenant.
/// </summary>
public sealed class Label : Entity, IWorkspaceScoped
{
    private readonly List<TaskLabel> _tasks = [];

    private Label() { }

    private Label(Guid id, Guid workspaceId, string name, string colorHex)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Name = name;
        ColorHex = colorHex;
    }

    public Guid WorkspaceId { get; private set; }

    public Workspace Workspace { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Six-digit hex, no leading hash. Stored normalised so the UI never has to guess.</summary>
    public string ColorHex { get; private set; } = null!;

    public IReadOnlyCollection<TaskLabel> Tasks => _tasks;

    public static Label Create(Guid workspaceId, string name, string colorHex) =>
        new(Guid.CreateVersion7(),
            Guard.NotDefault(workspaceId),
            Guard.MaxLength(Guard.NotEmpty(name), 40),
            NormalizeColor(colorHex));

    public void Rename(string name) => Name = Guard.MaxLength(Guard.NotEmpty(name), 40);

    public void Recolor(string colorHex) => ColorHex = NormalizeColor(colorHex);

    private static string NormalizeColor(string colorHex)
    {
        var value = Guard.NotEmpty(colorHex).TrimStart('#').ToUpperInvariant();
        Guard.Against(value.Length != 6, "Colour must be a 6-digit hex value.");
        Guard.Against(!value.All(char.IsAsciiHexDigit), "Colour must be a valid hex value.");
        return value;
    }
}
