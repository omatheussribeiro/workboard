using WorkBoard.Api.Filters;
using WorkBoard.Application.DTOs.Labels;
using WorkBoard.Application.UseCases.Labels.CreateLabel;
using WorkBoard.Application.UseCases.Labels.DeleteLabel;
using WorkBoard.Application.UseCases.Labels.ListLabels;
using WorkBoard.Application.UseCases.Labels.UpdateLabel;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Endpoints;

/// <summary>The workspace label taxonomy.</summary>
public static class LabelEndpoints
{
    private const string BasePath = "/api/workspaces/{workspaceId:guid}/labels";

    /// <summary>Maps the label routes.</summary>
    public static IEndpointRouteBuilder MapLabelEndpoints(this IEndpointRouteBuilder app)
    {
        var read = app.MapGroup(BasePath)
            .WithTags("Labels")
            .RequireAuthorization(WorkBoardClaims.WorkspaceMemberPolicy);

        read.MapGet("/", async (IListLabelsUseCase list, CancellationToken cancellationToken) =>
                Results.Ok(await list.ExecuteAsync(cancellationToken)))
            .WithName("ListLabels")
            .Produces<IReadOnlyList<LabelDto>>();

        // The taxonomy is shared by everyone in the workspace, so changing it is an admin action:
        // renaming a label everybody filters by is not a per-user preference.
        var write = app.MapGroup(BasePath)
            .WithTags("Labels")
            .RequireAuthorization(WorkBoardClaims.WorkspaceAdminPolicy);

        write.MapPost("/", async (
                CreateLabelRequest request,
                ICreateLabelUseCase create,
                CancellationToken cancellationToken) =>
            Results.Ok(await create.ExecuteAsync(request, cancellationToken)))
            .WithValidation<CreateLabelRequest>()
            .WithName("CreateLabel")
            .Produces<LabelDto>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        write.MapPut("/{labelId:guid}", async (
                Guid labelId,
                UpdateLabelRequest request,
                IUpdateLabelUseCase update,
                CancellationToken cancellationToken) =>
            {
                await update.ExecuteAsync(labelId, request, cancellationToken);
                return Results.NoContent();
            })
            .WithValidation<UpdateLabelRequest>()
            .WithName("UpdateLabel")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status409Conflict);

        write.MapDelete("/{labelId:guid}", async (
                Guid labelId,
                IDeleteLabelUseCase delete,
                CancellationToken cancellationToken) =>
            {
                await delete.ExecuteAsync(labelId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("DeleteLabel")
            .WithSummary("Deletes a label and untags every task carrying it.")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}
