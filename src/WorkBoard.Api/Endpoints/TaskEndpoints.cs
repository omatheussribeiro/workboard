using WorkBoard.Api.Contracts;
using WorkBoard.Api.Filters;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.UseCases.Tasks.ApplyTaskLabel;
using WorkBoard.Application.UseCases.Tasks.AssignTask;
using WorkBoard.Application.UseCases.Tasks.CreateTask;
using WorkBoard.Application.UseCases.Tasks.DeleteTask;
using WorkBoard.Application.UseCases.Tasks.GetTask;
using WorkBoard.Application.UseCases.Tasks.ListTasks;
using WorkBoard.Application.UseCases.Tasks.MoveTask;
using WorkBoard.Application.UseCases.Tasks.RemoveTaskLabel;
using WorkBoard.Application.UseCases.Tasks.UnassignTask;
using WorkBoard.Application.UseCases.Tasks.UpdateTask;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Endpoints;

/// <summary>Tasks: the list, the detail, and everything that changes one.</summary>
public static class TaskEndpoints
{
    private const string BasePath = "/api/workspaces/{workspaceId:guid}/tasks";

    /// <summary>Maps the task routes.</summary>
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        MapReads(app);
        MapWrites(app);
        MapRelations(app);

        return app;
    }

    private static RouteGroupBuilder Group(IEndpointRouteBuilder app) =>
        app.MapGroup(BasePath)
            .WithTags("Tasks")
            .RequireAuthorization(WorkBoardClaims.WorkspaceMemberPolicy);

    private static void MapReads(IEndpointRouteBuilder app)
    {
        var group = Group(app);

        group.MapGet("/", async (
                [AsParameters] TaskQueryParameters query,
                IListTasksUseCase list,
                CancellationToken cancellationToken) =>
            Results.Ok(await list.ExecuteAsync(query.ToFilter(), cancellationToken)))
            .WithName("ListTasks")
            .WithSummary("Filtered, paged task list.")
            .Produces<PagedResult<TaskListItemDto>>();

        group.MapGet("/{taskId:guid}", async (
                Guid taskId,
                IGetTaskUseCase get,
                CancellationToken cancellationToken) =>
            Results.Ok(await get.ExecuteAsync(taskId, cancellationToken)))
            .WithName("GetTask")
            .Produces<TaskDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapWrites(IEndpointRouteBuilder app)
    {
        var group = Group(app);

        group.MapPost("/", async (
                Guid workspaceId,
                CreateTaskRequest request,
                ICreateTaskUseCase create,
                CancellationToken cancellationToken) =>
            {
                var created = await create.ExecuteAsync(request, cancellationToken);
                return Results.Created($"/api/workspaces/{workspaceId}/tasks/{created.Id}", created);
            })
            .WithValidation<CreateTaskRequest>()
            .WithName("CreateTask")
            .Produces<TaskDetailDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{taskId:guid}", async (
                Guid taskId,
                UpdateTaskRequest request,
                IUpdateTaskUseCase update,
                CancellationToken cancellationToken) =>
            Results.Ok(await update.ExecuteAsync(taskId, request, cancellationToken)))
            .WithValidation<UpdateTaskRequest>()
            .WithName("UpdateTask")
            .WithSummary("Updates a task. Requires the concurrency token from the last read.")
            .Produces<TaskDetailDto>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{taskId:guid}/position", async (
                Guid taskId,
                MoveTaskRequest request,
                IMoveTaskUseCase move,
                CancellationToken cancellationToken) =>
            Results.Ok(await move.ExecuteAsync(taskId, request, cancellationToken)))
            .WithValidation<MoveTaskRequest>()
            .WithName("MoveTask")
            .WithSummary("Applies a drag-and-drop move between or within columns.")
            .Produces<TaskDetailDto>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{taskId:guid}", async (
                Guid taskId,
                IDeleteTaskUseCase delete,
                CancellationToken cancellationToken) =>
            {
                await delete.ExecuteAsync(taskId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("DeleteTask")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    /// <summary>Assignees and labels: relations rather than fields, so they get their own routes.</summary>
    private static void MapRelations(IEndpointRouteBuilder app)
    {
        var group = Group(app);

        group.MapPost("/{taskId:guid}/assignees", async (
                Guid taskId,
                AssignTaskRequest request,
                IAssignTaskUseCase assign,
                CancellationToken cancellationToken) =>
            {
                await assign.ExecuteAsync(taskId, request, cancellationToken);
                return Results.NoContent();
            })
            .WithName("AssignTask")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{taskId:guid}/assignees/{userId:guid}", async (
                Guid taskId,
                Guid userId,
                IUnassignTaskUseCase unassign,
                CancellationToken cancellationToken) =>
            {
                await unassign.ExecuteAsync(taskId, userId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("UnassignTask")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPut("/{taskId:guid}/labels/{labelId:guid}", async (
                Guid taskId,
                Guid labelId,
                IApplyTaskLabelUseCase applyLabel,
                CancellationToken cancellationToken) =>
            {
                await applyLabel.ExecuteAsync(taskId, labelId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("ApplyTaskLabel")
            .Produces(StatusCodes.Status204NoContent);

        group.MapDelete("/{taskId:guid}/labels/{labelId:guid}", async (
                Guid taskId,
                Guid labelId,
                IRemoveTaskLabelUseCase removeLabel,
                CancellationToken cancellationToken) =>
            {
                await removeLabel.ExecuteAsync(taskId, labelId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("RemoveTaskLabel")
            .Produces(StatusCodes.Status204NoContent);
    }
}
