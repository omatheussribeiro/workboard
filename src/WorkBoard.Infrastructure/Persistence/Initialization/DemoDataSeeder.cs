using WorkBoard.Infrastructure.Authentication;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Initialization;

/// <summary>
/// Demonstration data for Development.
/// </summary>
/// <remarks>
/// <para>
/// Two workspaces, not one. A single-tenant seed cannot show the thing this project is built around:
/// with two populated workspaces, signing in as a member of one and requesting the other returns
/// nothing at all, which is the isolation story made visible rather than asserted. The integration
/// tests rely on the same shape.
/// </para>
/// <para>
/// The seed passwords are fixed and public on purpose — this runs only when
/// <c>Database:SeedDemoData</c> is enabled, which the Production settings never do.
/// </para>
/// </remarks>
internal static class DemoDataSeeder
{
    public const string DemoPassword = "WorkBoard!2025";

    public static async Task SeedAsync(WorkBoardDbContext context, CancellationToken cancellationToken)
    {
        var hasher = new BCryptPasswordHasher();

        // Hashed once and reused. BCrypt at cost 12 takes about a quarter of a second, and hashing
        // it per user would make a container start noticeably slower for no benefit.
        var passwordHash = hasher.Hash(DemoPassword);

        var ada = User.Register("ada@workboard.dev", "Ada Lovelace", passwordHash);
        var grace = User.Register("grace@workboard.dev", "Grace Hopper", passwordHash);
        var alan = User.Register("alan@workboard.dev", "Alan Turing", passwordHash);
        var outsider = User.Register("outsider@other.dev", "Rival Corp", passwordHash);

        context.Users.AddRange(ada, grace, alan, outsider);

        var acme = Workspace.Create("Acme Product", "acme", ada.Id);
        acme.Describe("The demo workspace. Ada owns it; Grace and Alan are members.");
        acme.AddMember(grace.Id, WorkspaceRole.Admin);
        acme.AddMember(alan.Id, WorkspaceRole.Member);

        // Exists solely so that isolation is demonstrable: its data must be invisible from Acme.
        var rival = Workspace.Create("Rival Corp", "rival", outsider.Id);
        rival.Describe("A second tenant. Nothing here may ever be visible from Acme.");

        context.Workspaces.AddRange(acme, rival);

        var bug = Label.Create(acme.Id, "bug", "EF4444");
        var feature = Label.Create(acme.Id, "feature", "3B82F6");
        var techDebt = Label.Create(acme.Id, "tech-debt", "F59E0B");
        context.Labels.AddRange(bug, feature, techDebt);

        var platform = SeedProject(context, acme.Id, "Platform", "PLAT", ada.Id, [bug, techDebt]);
        var mobile = SeedProject(context, acme.Id, "Mobile App", "MOB", grace.Id, [feature]);

        AssignAcross(platform, [ada.Id, grace.Id, alan.Id]);
        AssignAcross(mobile, [grace.Id, alan.Id]);

        // The other tenant gets real content too — an empty workspace would prove nothing.
        SeedProject(context, rival.Id, "Secret Project", "SEC", outsider.Id, []);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Project SeedProject(
        WorkBoardDbContext context,
        Guid workspaceId,
        string name,
        string key,
        Guid ownerId,
        IReadOnlyList<Label> labels)
    {
        var project = Project.CreateWithDefaultBoard(workspaceId, name, key, ownerId);
        project.Describe($"Demonstration project seeded for {name}.");

        var board = project.Boards.First();
        var columns = board.Columns.OrderBy(c => c.Position).ToList();

        var titles = new (string Title, int ColumnIndex, TaskPriority Priority, int? DueInDays)[]
        {
            ("Set up CI pipeline", 2, TaskPriority.High, null),
            ("Design the workspace switcher", 1, TaskPriority.Medium, 5),
            ("Add optimistic concurrency to task moves", 1, TaskPriority.Critical, 2),
            ("Write the isolation test suite", 0, TaskPriority.High, 9),
            ("Replace the ad-hoc search with full-text", 0, TaskPriority.Low, null),
            ("Fix the drag-and-drop ghost on Safari", 0, TaskPriority.Medium, 14),
        };

        var now = DateTimeOffset.UtcNow;

        foreach (var (title, columnIndex, priority, dueInDays) in titles)
        {
            var column = columns[columnIndex];
            var task = project.CreateTask(column.Id, title, ownerId);

            task.ChangeDescription($"Seeded task: {title}.");
            task.ChangePriority(priority, ownerId);

            if (dueInDays is { } days)
            {
                task.SetDeadline(now.AddDays(days), now, ownerId);
            }

            var lastPosition = project.Tasks
                .Where(t => t.BoardColumnId == column.Id && t.Id != task.Id)
                .Select(t => (double?)t.Position)
                .Max();

            task.PlaceAtEndOf(column, lastPosition);

            // Walk the task through the column it was seeded into, so StartedAtUtc and CompletedAtUtc
            // reflect its state. Creating it directly in a Done column would otherwise leave the
            // dashboard showing finished work with no completion date.
            task.MoveTo(column, column, lastPosition, null, now, ownerId);

            foreach (var label in labels.Take(priority is TaskPriority.Critical or TaskPriority.High ? 2 : 1))
            {
                task.ApplyLabel(label);
            }
        }

        context.Projects.Add(project);
        return project;
    }

    private static void AssignAcross(Project project, IReadOnlyList<Guid> userIds)
    {
        var index = 0;

        foreach (var task in project.Tasks)
        {
            task.Assign(userIds[index % userIds.Count], userIds[0]);
            index++;
        }
    }
}
