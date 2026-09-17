using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Application.DTOs.Boards;
using WorkBoard.Application.DTOs.Projects;
using WorkBoard.Application.DTOs.Workspaces;

namespace WorkBoard.IntegrationTests;

/// <summary>
/// Builds the data a test needs through the public API, never by reaching into the database.
/// </summary>
/// <remarks>
/// Writing fixtures through the API means the setup exercises the same validation, authorisation and
/// tenant binding as production. Inserting rows directly would be faster and would let a test set up
/// a state the application can never produce — which is how a suite ends up passing against data that
/// cannot exist.
/// </remarks>
public sealed class Scenario(WorkBoardApiFactory factory)
{
    /// <summary>Registers a new account and returns a client already authenticated as them.</summary>
    public async Task<(ApiClient Client, AuthenticatedUserDto User, string RefreshToken)> NewUserAsync(string? label = null)
    {
        var client = new ApiClient(factory.CreateClient());
        var suffix = Guid.NewGuid().ToString("N")[..12];

        var auth = await client.PostAsync<RegisterRequest, AuthResponse>("/api/auth/register", new RegisterRequest(
            $"{label ?? "user"}-{suffix}@workboard.test",
            $"{label ?? "User"} {suffix[..4]}",
            "integration-test-password-1"));

        client.Authenticate(auth.AccessToken);
        return (client, auth.User, auth.RefreshToken);
    }

    public static async Task<WorkspaceSummaryDto> NewWorkspaceAsync(ApiClient client, string name = "Acme")
    {
        // Unique per run, and within the 60-character limit the domain enforces on a slug.
        var slug = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}"[..20];

        return await client.PostAsync<CreateWorkspaceRequest, WorkspaceSummaryDto>(
            "/api/workspaces",
            new CreateWorkspaceRequest(name, slug, null));
    }

    public static async Task<ProjectSummaryDto> NewProjectAsync(ApiClient client, Guid workspaceId, string key = "PLAT") =>
        await client.PostAsync<CreateProjectRequest, ProjectSummaryDto>(
            $"/api/workspaces/{workspaceId}/projects",
            new CreateProjectRequest($"Project {key}", key, null));

    public static async Task<BoardDto> BoardAsync(ApiClient client, Guid workspaceId, Guid boardId) =>
        await client.GetAsync<BoardDto>($"/api/workspaces/{workspaceId}/boards/{boardId}");

    /// <summary>A workspace with one project, its board, and the three default columns.</summary>
    public async Task<WorkspaceFixture> NewWorkspaceWithProjectAsync(string? label = null)
    {
        var (client, user, _) = await NewUserAsync(label);
        var workspace = await NewWorkspaceAsync(client);
        var project = await NewProjectAsync(client, workspace.Id);
        var board = await BoardAsync(client, workspace.Id, project.DefaultBoardId!.Value);

        return new WorkspaceFixture(client, user, workspace, project, board);
    }
}

public sealed record WorkspaceFixture(
    ApiClient Client,
    AuthenticatedUserDto User,
    WorkspaceSummaryDto Workspace,
    ProjectSummaryDto Project,
    BoardDto Board)
{
    public BoardColumnDto Column(WorkBoard.Domain.Enums.TaskStatusCategory category) =>
        Board.Columns.Single(c => c.Category == category);

    public Guid TodoColumnId => Column(WorkBoard.Domain.Enums.TaskStatusCategory.Todo).Id;

    public Guid InProgressColumnId => Column(WorkBoard.Domain.Enums.TaskStatusCategory.InProgress).Id;

    public Guid DoneColumnId => Column(WorkBoard.Domain.Enums.TaskStatusCategory.Done).Id;
}
