using WorkBoard.Api.Filters;
using WorkBoard.Application.DTOs.Boards;
using WorkBoard.Application.UseCases.Boards.AddColumn;
using WorkBoard.Application.UseCases.Boards.GetBoard;
using WorkBoard.Application.UseCases.Boards.MoveColumn;
using WorkBoard.Application.UseCases.Boards.RemoveColumn;
using WorkBoard.Application.UseCases.Boards.UpdateColumn;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Endpoints;

/// <summary>The Kanban board and its columns.</summary>
public static class BoardEndpoints
{
    /// <summary>Maps the board routes.</summary>
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces/{workspaceId:guid}/boards")
            .WithTags("Boards")
            .RequireAuthorization(WorkBoardClaims.WorkspaceMemberPolicy);

        group.MapGet("/{boardId:guid}", async (
                Guid boardId,
                IGetBoardUseCase get,
                CancellationToken cancellationToken) =>
            Results.Ok(await get.ExecuteAsync(boardId, cancellationToken)))
            .WithName("GetBoard")
            .WithSummary("The whole Kanban board: columns, cards, assignees and labels, in one request.")
            .Produces<BoardDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{boardId:guid}/columns", async (
                Guid boardId,
                CreateColumnRequest request,
                IAddColumnUseCase addColumn,
                CancellationToken cancellationToken) =>
            {
                var columnId = await addColumn.ExecuteAsync(boardId, request, cancellationToken);
                return Results.Ok(new { columnId });
            })
            .WithValidation<CreateColumnRequest>()
            .WithName("AddColumn")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{boardId:guid}/columns/{columnId:guid}", async (
                Guid boardId,
                Guid columnId,
                UpdateColumnRequest request,
                IUpdateColumnUseCase updateColumn,
                CancellationToken cancellationToken) =>
            {
                await updateColumn.ExecuteAsync(boardId, columnId, request, cancellationToken);
                return Results.NoContent();
            })
            .WithValidation<UpdateColumnRequest>()
            .WithName("UpdateColumn")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPut("/{boardId:guid}/columns/{columnId:guid}/position", async (
                Guid boardId,
                Guid columnId,
                MoveColumnRequest request,
                IMoveColumnUseCase moveColumn,
                CancellationToken cancellationToken) =>
            {
                await moveColumn.ExecuteAsync(boardId, columnId, request, cancellationToken);
                return Results.NoContent();
            })
            .WithName("MoveColumn")
            .Produces(StatusCodes.Status204NoContent);

        group.MapDelete("/{boardId:guid}/columns/{columnId:guid}", async (
                Guid boardId,
                Guid columnId,
                IRemoveColumnUseCase removeColumn,
                CancellationToken cancellationToken) =>
            {
                await removeColumn.ExecuteAsync(boardId, columnId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("RemoveColumn")
            .WithSummary("Removes an empty column. Refuses while it still holds cards.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}
