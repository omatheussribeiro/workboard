using System.Net;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Activities;
using WorkBoard.Application.DTOs.Boards;
using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.DTOs.Projects;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Domain.Enums;

namespace WorkBoard.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class TaskWorkflowTests(WorkBoardApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Creating_a_project_gives_it_a_board_with_three_columns()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();

        fixture.Project.DefaultBoardId.Should().NotBeNull();
        fixture.Board.Columns.Select(c => c.Category)
            .Should().Equal(TaskStatusCategory.Todo, TaskStatusCategory.InProgress, TaskStatusCategory.Done);
    }

    [Fact]
    public async Task A_project_key_is_unique_within_a_workspace_but_not_across_them()
    {
        var acme = await _scenario.NewWorkspaceWithProjectAsync("acme");

        // A second tenant with no project yet, so the only thing under test is the key.
        var (rivalClient, _, _) = await _scenario.NewUserAsync("rival");
        var rivalWorkspace = await Scenario.NewWorkspaceAsync(rivalClient, "Rival");

        var duplicate = await acme.Client.PostAsync(
            $"/api/workspaces/{acme.Workspace.Id}/projects",
            new CreateProjectRequest("Another", acme.Project.Key, null));

        var sameKeyElsewhere = await rivalClient.PostAsync(
            $"/api/workspaces/{rivalWorkspace.Id}/projects",
            new CreateProjectRequest("Same key, other tenant", acme.Project.Key, null));

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        sameKeyElsewhere.StatusCode.Should().Be(HttpStatusCode.Created,
            "keys are unique per workspace, not globally — two tenants may both want PLAT");
    }

    [Fact]
    public async Task Tasks_are_numbered_per_project()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();

        var first = await CreateTaskAsync(fixture, "One");
        var second = await CreateTaskAsync(fixture, "Two");

        first.Key.Should().Be($"{fixture.Project.Key}-1");
        second.Key.Should().Be($"{fixture.Project.Key}-2");
    }

    [Fact]
    public async Task A_new_task_lands_at_the_end_of_its_column()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();

        await CreateTaskAsync(fixture, "First");
        await CreateTaskAsync(fixture, "Second");

        var board = await Scenario.BoardAsync(fixture.Client, fixture.Workspace.Id, fixture.Board.Id);
        var todo = board.Columns.Single(c => c.Category == TaskStatusCategory.Todo);

        todo.Cards.Select(c => c.Title).Should().Equal("First", "Second");
        todo.Cards[0].Position.Should().BeLessThan(todo.Cards[1].Position);
    }

    [Fact]
    public async Task Moving_a_task_to_Done_records_the_completion()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();
        var task = await CreateTaskAsync(fixture, "Ship it");

        var moved = await MoveAsync(fixture, task, fixture.DoneColumnId);

        moved.Category.Should().Be(TaskStatusCategory.Done);
        moved.CompletedAtUtc.Should().NotBeNull();
        moved.StartedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task A_move_is_visible_on_the_next_board_read()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();
        var task = await CreateTaskAsync(fixture, "Ship it");

        await MoveAsync(fixture, task, fixture.InProgressColumnId);

        var board = await Scenario.BoardAsync(fixture.Client, fixture.Workspace.Id, fixture.Board.Id);

        board.Columns.Single(c => c.Category == TaskStatusCategory.Todo).Cards.Should().BeEmpty();
        board.Columns.Single(c => c.Category == TaskStatusCategory.InProgress)
            .Cards.Should().ContainSingle().Which.Title.Should().Be("Ship it");
    }

    /// <summary>
    /// Two people drag the same card; the second one is told rather than silently overwriting.
    /// </summary>
    /// <remarks>
    /// Both clients read the board, so both hold the same <c>rowversion</c>. The first move succeeds
    /// and SQL Server bumps the token; the second carries a value that no longer matches, its UPDATE
    /// affects zero rows, and EF raises a concurrency exception that surfaces as 409.
    /// </remarks>
    [Fact]
    public async Task A_concurrent_move_with_a_stale_token_is_rejected_with_409()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();
        var task = await CreateTaskAsync(fixture, "Contended");

        var staleToken = task.RowVersion;

        var first = await MoveAsync(fixture, task, fixture.InProgressColumnId);
        first.Category.Should().Be(TaskStatusCategory.InProgress);

        var second = await fixture.Client.PutAsync(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks/{task.Id}/position",
            new MoveTaskRequest(fixture.DoneColumnId, null, null, staleToken));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_concurrent_edit_with_a_stale_token_is_rejected_with_409()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();
        var task = await CreateTaskAsync(fixture, "Contended");
        var staleToken = task.RowVersion;

        var updated = await fixture.Client.PutAsync<UpdateTaskRequest, TaskDetailDto>(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks/{task.Id}",
            new UpdateTaskRequest("First writer wins", null, TaskPriority.High, null, staleToken));

        updated.Title.Should().Be("First writer wins");

        var second = await fixture.Client.PutAsync(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks/{task.Id}",
            new UpdateTaskRequest("Second writer", null, TaskPriority.Low, null, staleToken));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_fresh_token_after_a_conflict_lets_the_caller_retry()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();
        var task = await CreateTaskAsync(fixture, "Retry me");

        await MoveAsync(fixture, task, fixture.InProgressColumnId);

        // The client is told to reload; reloading is what makes the retry succeed.
        var reloaded = await fixture.Client.GetAsync<TaskDetailDto>(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks/{task.Id}");

        var retried = await MoveAsync(fixture, reloaded, fixture.DoneColumnId);

        retried.Category.Should().Be(TaskStatusCategory.Done);
    }

    [Fact]
    public async Task A_deadline_in_the_past_is_refused_with_422()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();
        var task = await CreateTaskAsync(fixture, "Late");

        var response = await fixture.Client.PutAsync(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks/{task.Id}",
            new UpdateTaskRequest(
                task.Title,
                null,
                TaskPriority.Medium,
                DateTimeOffset.UtcNow.AddDays(-1),
                task.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "a well-formed request that breaks a business rule is 422, not 400");
    }

    [Fact]
    public async Task A_column_at_its_WIP_limit_refuses_another_card()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();

        var columnResponse = await fixture.Client.PostAsync(
            $"/api/workspaces/{fixture.Workspace.Id}/boards/{fixture.Board.Id}/columns",
            new CreateColumnRequest("Review", TaskStatusCategory.InProgress, WipLimit: 1));

        var columnId = (await ApiClient.ReadAsync<ColumnCreated>(columnResponse)).ColumnId;

        var first = await CreateTaskAsync(fixture, "One");
        var second = await CreateTaskAsync(fixture, "Two");

        (await MoveAsync(fixture, first, columnId)).BoardColumnId.Should().Be(columnId);

        var blocked = await fixture.Client.PutAsync(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks/{second.Id}/position",
            new MoveTaskRequest(columnId, null, null, second.RowVersion));

        blocked.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Filters_narrow_the_task_list()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();

        await CreateTaskAsync(fixture, "Low priority", TaskPriority.Low);
        await CreateTaskAsync(fixture, "Critical one", TaskPriority.Critical);

        var critical = await fixture.Client.GetAsync<PagedResult<TaskListItemDto>>(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks?priority=Critical");

        critical.TotalCount.Should().Be(1);
        critical.Items.Should().ContainSingle().Which.Title.Should().Be("Critical one");
    }

    [Fact]
    public async Task Paging_returns_each_task_exactly_once()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();

        for (var i = 1; i <= 7; i++)
        {
            // All the same priority on purpose: ties in the sort key are where an unstable ORDER BY
            // starts repeating rows across pages.
            await CreateTaskAsync(fixture, $"Task {i}", TaskPriority.Medium);
        }

        var first = await fixture.Client.GetAsync<PagedResult<TaskListItemDto>>(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks?sort=Priority&page=1&pageSize=3");
        var second = await fixture.Client.GetAsync<PagedResult<TaskListItemDto>>(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks?sort=Priority&page=2&pageSize=3");
        var third = await fixture.Client.GetAsync<PagedResult<TaskListItemDto>>(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks?sort=Priority&page=3&pageSize=3");

        var ids = first.Items.Concat(second.Items).Concat(third.Items).Select(t => t.Id).ToList();

        ids.Should().HaveCount(7).And.OnlyHaveUniqueItems();
        first.TotalCount.Should().Be(7);
    }

    [Fact]
    public async Task An_oversized_page_request_is_clamped()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();
        await CreateTaskAsync(fixture, "One");

        var page = await fixture.Client.GetAsync<PagedResult<TaskListItemDto>>(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks?pageSize=100000");

        page.PageSize.Should().Be(PageRequest.MaxPageSize);
    }

    [Fact]
    public async Task Every_change_to_a_task_lands_in_its_history()
    {
        var fixture = await _scenario.NewWorkspaceWithProjectAsync();
        var task = await CreateTaskAsync(fixture, "Tracked");

        await MoveAsync(fixture, task, fixture.InProgressColumnId);

        var history = await fixture.Client.GetAsync<PagedResult<ActivityDto>>(
            $"/api/workspaces/{fixture.Workspace.Id}/activities?taskId={task.Id}");

        history.Items.Select(a => a.Type)
            .Should().Contain([WorkBoard.Domain.Enums.ActivityType.TaskCreated,
                               WorkBoard.Domain.Enums.ActivityType.TaskMoved]);
    }

    [Fact]
    public async Task Commenting_notifies_the_other_assignees_but_not_the_author()
    {
        var owner = await _scenario.NewWorkspaceWithProjectAsync("owner");
        var (memberClient, member, _) = await _scenario.NewUserAsync("teammate");

        await owner.Client.PostAsync(
            $"/api/workspaces/{owner.Workspace.Id}/members",
            new WorkBoard.Application.DTOs.Workspaces.AddMemberRequest(
                member.Email,
                WorkBoard.Domain.Enums.WorkspaceRole.Member));

        var task = await CreateTaskAsync(owner, "Discuss this");

        await owner.Client.PostAsync(
            $"/api/workspaces/{owner.Workspace.Id}/tasks/{task.Id}/assignees",
            new AssignTaskRequest(member.Id));

        await owner.Client.PostAsync(
            $"/api/workspaces/{owner.Workspace.Id}/tasks/{task.Id}/comments",
            new CreateCommentRequest("What do you think?"));

        var theirs = await memberClient.GetAsync<UnreadCount>(
            $"/api/workspaces/{owner.Workspace.Id}/notifications/unread-count");
        var mine = await owner.Client.GetAsync<UnreadCount>(
            $"/api/workspaces/{owner.Workspace.Id}/notifications/unread-count");

        theirs.Count.Should().BeGreaterThan(0);
        mine.Count.Should().Be(0, "nobody needs to be told what they just did");
    }

    [Fact]
    public async Task A_comment_can_only_be_edited_by_its_author()
    {
        var owner = await _scenario.NewWorkspaceWithProjectAsync("owner");
        var (memberClient, member, _) = await _scenario.NewUserAsync("teammate");

        await owner.Client.PostAsync(
            $"/api/workspaces/{owner.Workspace.Id}/members",
            new WorkBoard.Application.DTOs.Workspaces.AddMemberRequest(
                member.Email,
                WorkBoard.Domain.Enums.WorkspaceRole.Member));

        var task = await CreateTaskAsync(owner, "Discuss this");

        var comment = await owner.Client.PostAsync<CreateCommentRequest, CommentDto>(
            $"/api/workspaces/{owner.Workspace.Id}/tasks/{task.Id}/comments",
            new CreateCommentRequest("Mine"));

        var response = await memberClient.PutAsync(
            $"/api/workspaces/{owner.Workspace.Id}/comments/{comment.Id}",
            new UpdateCommentRequest("Not yours"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static async Task<TaskDetailDto> CreateTaskAsync(
        WorkspaceFixture fixture,
        string title,
        TaskPriority priority = TaskPriority.Medium) =>
        await fixture.Client.PostAsync<CreateTaskRequest, TaskDetailDto>(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks",
            new CreateTaskRequest(fixture.Project.Id, fixture.TodoColumnId, title, null, priority, null, null, null, null));

    private static async Task<TaskDetailDto> MoveAsync(WorkspaceFixture fixture, TaskDetailDto task, Guid targetColumnId) =>
        await fixture.Client.PutAsync<MoveTaskRequest, TaskDetailDto>(
            $"/api/workspaces/{fixture.Workspace.Id}/tasks/{task.Id}/position",
            new MoveTaskRequest(targetColumnId, null, null, task.RowVersion));

    private sealed record ColumnCreated(Guid ColumnId);

    private sealed record UnreadCount(int Count);
}
