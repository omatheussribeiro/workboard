using System.Net;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Domain.Enums;

namespace WorkBoard.IntegrationTests;

/// <summary>
/// The security tests the whole design exists for.
/// </summary>
/// <remarks>
/// Multi-tenancy is easy to claim and easy to get subtly wrong: one handler that forgets a
/// <c>Where</c>, one id accepted from the body instead of the route. These tests set up two real
/// tenants and try, from one, every way of reaching the other.
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class WorkspaceIsolationTests(WorkBoardApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task A_user_only_sees_the_workspaces_they_belong_to()
    {
        var acme = await _scenario.NewWorkspaceWithProjectAsync("acme");
        await _scenario.NewWorkspaceWithProjectAsync("rival");

        var mine = await acme.Client.GetAsync<List<WorkspaceSummaryDto>>("/api/workspaces");

        mine.Should().ContainSingle().Which.Id.Should().Be(acme.Workspace.Id);
    }

    [Fact]
    public async Task An_outsider_cannot_read_another_workspace()
    {
        var acme = await _scenario.NewWorkspaceWithProjectAsync("acme");
        var rival = await _scenario.NewWorkspaceWithProjectAsync("rival");

        var response = await rival.Client.GetAsync($"/api/workspaces/{acme.Workspace.Id}/projects");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// The subtle one, and the reason the tenant filter exists as well as the policy.
    /// </summary>
    /// <remarks>
    /// Here the caller is a legitimate member of the workspace in the route, so the authorisation
    /// policy passes. They then supply an id belonging to a different tenant. Nothing in the handler
    /// checks the workspace — the global query filter makes the row simply not exist, and the result
    /// is a 404 rather than someone else's data.
    /// </remarks>
    [Fact]
    public async Task An_id_from_another_workspace_does_not_resolve_even_under_a_valid_route()
    {
        var acme = await _scenario.NewWorkspaceWithProjectAsync("acme");
        var rival = await _scenario.NewWorkspaceWithProjectAsync("rival");

        var project = await acme.Client.GetAsync(
            $"/api/workspaces/{acme.Workspace.Id}/projects/{rival.Project.Id}");

        var board = await acme.Client.GetAsync(
            $"/api/workspaces/{acme.Workspace.Id}/boards/{rival.Board.Id}");

        project.StatusCode.Should().Be(HttpStatusCode.NotFound);
        board.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_task_from_another_workspace_does_not_resolve()
    {
        var acme = await _scenario.NewWorkspaceWithProjectAsync("acme");
        var rival = await _scenario.NewWorkspaceWithProjectAsync("rival");

        var rivalTask = await CreateTaskAsync(rival, "Rival secret");

        var response = await acme.Client.GetAsync($"/api/workspaces/{acme.Workspace.Id}/tasks/{rivalTask.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_task_cannot_be_created_into_another_workspaces_project()
    {
        var acme = await _scenario.NewWorkspaceWithProjectAsync("acme");
        var rival = await _scenario.NewWorkspaceWithProjectAsync("rival");

        var response = await acme.Client.PostAsync(
            $"/api/workspaces/{acme.Workspace.Id}/tasks",
            new CreateTaskRequest(
                rival.Project.Id,
                rival.TodoColumnId,
                "Planted",
                null,
                TaskPriority.Medium,
                null,
                null,
                null,
                null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "the project id does not resolve under this tenant, so there is nothing to create into");
    }

    [Fact]
    public async Task Search_never_crosses_a_tenant_boundary()
    {
        var acme = await _scenario.NewWorkspaceWithProjectAsync("acme");
        var rival = await _scenario.NewWorkspaceWithProjectAsync("rival");

        var marker = $"marker{Guid.NewGuid():N}";
        await CreateTaskAsync(rival, $"Rival {marker} plan");

        var results = await acme.Client.GetAsync<WorkBoard.Application.DTOs.Search.SearchResultsDto>(
            $"/api/workspaces/{acme.Workspace.Id}/search?q={marker}");

        results.Tasks.Should().BeEmpty();
        results.Projects.Should().BeEmpty();
        results.Comments.Should().BeEmpty();
    }

    [Fact]
    public async Task The_dashboard_counts_only_the_current_tenant()
    {
        var acme = await _scenario.NewWorkspaceWithProjectAsync("acme");
        var rival = await _scenario.NewWorkspaceWithProjectAsync("rival");

        await CreateTaskAsync(acme, "Ours");
        await CreateTaskAsync(rival, "Theirs one");
        await CreateTaskAsync(rival, "Theirs two");

        var dashboard = await acme.Client.GetAsync<WorkBoard.Application.DTOs.Dashboard.DashboardDto>(
            $"/api/workspaces/{acme.Workspace.Id}/dashboard");

        dashboard.Totals.Tasks.Should().Be(1);
        dashboard.Totals.Projects.Should().Be(1);
        dashboard.Totals.Members.Should().Be(1);
    }

    [Fact]
    public async Task The_activity_feed_shows_only_the_current_tenant()
    {
        var acme = await _scenario.NewWorkspaceWithProjectAsync("acme");
        var rival = await _scenario.NewWorkspaceWithProjectAsync("rival");

        var marker = $"marker{Guid.NewGuid():N}";
        await CreateTaskAsync(rival, $"Rival {marker}");

        var feed = await acme.Client.GetAsync<
            WorkBoard.Application.DTOs.Common.PagedResult<WorkBoard.Application.DTOs.Activities.ActivityDto>>(
            $"/api/workspaces/{acme.Workspace.Id}/activities?pageSize=100");

        feed.Items.Should().NotContain(a => a.Summary.Contains(marker, StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_member_cannot_perform_an_admin_action()
    {
        var owner = await _scenario.NewWorkspaceWithProjectAsync("owner");
        var (memberClient, member, _) = await _scenario.NewUserAsync("member");

        await owner.Client.PostAsync(
            $"/api/workspaces/{owner.Workspace.Id}/members",
            new AddMemberRequest(member.Email, WorkspaceRole.Member));

        // A member can read the board...
        var read = await memberClient.GetAsync($"/api/workspaces/{owner.Workspace.Id}/projects");
        read.StatusCode.Should().Be(HttpStatusCode.OK);

        // ...but cannot change who else is in the workspace.
        var write = await memberClient.PostAsync(
            $"/api/workspaces/{owner.Workspace.Id}/members",
            new AddMemberRequest("someone-else@workboard.test", WorkspaceRole.Member));

        write.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_admin_can_perform_admin_actions()
    {
        var owner = await _scenario.NewWorkspaceWithProjectAsync("owner");
        var (adminClient, admin, _) = await _scenario.NewUserAsync("admin");

        await owner.Client.PostAsync(
            $"/api/workspaces/{owner.Workspace.Id}/members",
            new AddMemberRequest(admin.Email, WorkspaceRole.Admin));

        var archive = await adminClient.DeleteAsync(
            $"/api/workspaces/{owner.Workspace.Id}/projects/{owner.Project.Id}");

        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Membership is read per request, not carried in the token.
    /// </summary>
    /// <remarks>
    /// The removed user still holds a perfectly valid, unexpired access token. If roles lived in the
    /// token they would keep their access until it expired; because membership is a database lookup,
    /// the very next request is refused.
    /// </remarks>
    [Fact]
    public async Task Removing_a_member_takes_effect_immediately_on_their_existing_token()
    {
        var owner = await _scenario.NewWorkspaceWithProjectAsync("owner");
        var (memberClient, member, _) = await _scenario.NewUserAsync("revoked");

        await owner.Client.PostAsync(
            $"/api/workspaces/{owner.Workspace.Id}/members",
            new AddMemberRequest(member.Email, WorkspaceRole.Member));

        (await memberClient.GetAsync($"/api/workspaces/{owner.Workspace.Id}/projects"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await owner.Client.DeleteAsync($"/api/workspaces/{owner.Workspace.Id}/members/{member.Id}");

        (await memberClient.GetAsync($"/api/workspaces/{owner.Workspace.Id}/projects"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Someone_outside_the_workspace_cannot_be_assigned_a_task()
    {
        var acme = await _scenario.NewWorkspaceWithProjectAsync("acme");
        var (_, outsider, _) = await _scenario.NewUserAsync("outsider");

        var task = await CreateTaskAsync(acme, "Ours");

        var response = await acme.Client.PostAsync(
            $"/api/workspaces/{acme.Workspace.Id}/tasks/{task.Id}/assignees",
            new AssignTaskRequest(outsider.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "a query filter constrains reads, not what an insert may reference");
    }

    private static async Task<TaskDetailDto> CreateTaskAsync(WorkspaceFixture fixture, string title) =>
        await fixture.Client.PostAsync<CreateTaskRequest, TaskDetailDto>(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks",
            new CreateTaskRequest(
                fixture.Project.Id,
                fixture.TodoColumnId,
                title,
                null,
                TaskPriority.Medium,
                null,
                null,
                null,
                null));
}
