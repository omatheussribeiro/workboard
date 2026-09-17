using WorkBoard.Api.Filters;
using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.UseCases.Comments.AddComment;
using WorkBoard.Application.UseCases.Comments.DeleteComment;
using WorkBoard.Application.UseCases.Comments.ListComments;
using WorkBoard.Application.UseCases.Comments.UpdateComment;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Endpoints;

/// <summary>Comment threads on tasks.</summary>
public static class CommentEndpoints
{
    private const int DefaultPageSize = 50;

    /// <summary>Maps the comment routes.</summary>
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces/{workspaceId:guid}")
            .WithTags("Comments")
            .RequireAuthorization(WorkBoardClaims.WorkspaceMemberPolicy);

        group.MapGet("/tasks/{taskId:guid}/comments", async (
                Guid taskId,
                int? page,
                int? pageSize,
                IListCommentsUseCase list,
                CancellationToken cancellationToken) =>
            Results.Ok(await list.ExecuteAsync(
                taskId,
                new PageRequest(page ?? 1, pageSize ?? DefaultPageSize),
                cancellationToken)))
            .WithName("ListComments")
            .Produces<PagedResult<CommentDto>>();

        group.MapPost("/tasks/{taskId:guid}/comments", async (
                Guid taskId,
                CreateCommentRequest request,
                IAddCommentUseCase add,
                CancellationToken cancellationToken) =>
            Results.Ok(await add.ExecuteAsync(taskId, request, cancellationToken)))
            .WithValidation<CreateCommentRequest>()
            .WithName("AddComment")
            .Produces<CommentDto>();

        group.MapPut("/comments/{commentId:guid}", async (
                Guid commentId,
                UpdateCommentRequest request,
                IUpdateCommentUseCase update,
                CancellationToken cancellationToken) =>
            {
                await update.ExecuteAsync(commentId, request, cancellationToken);
                return Results.NoContent();
            })
            .WithValidation<UpdateCommentRequest>()
            .WithName("UpdateComment")
            .WithSummary("Edits a comment. Author only.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/comments/{commentId:guid}", async (
                Guid commentId,
                IDeleteCommentUseCase delete,
                CancellationToken cancellationToken) =>
            {
                await delete.ExecuteAsync(commentId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("DeleteComment")
            .WithSummary("Soft-deletes a comment. Author or workspace admin.")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}
