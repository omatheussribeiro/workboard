using WorkBoard.Application.DTOs.Activities;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.UseCases.Activities.ListActivities;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Endpoints;

/// <summary>The change history.</summary>
public static class ActivityEndpoints
{
    private const int DefaultPageSize = 30;

    /// <summary>Maps the activity route.</summary>
    public static IEndpointRouteBuilder MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/{workspaceId:guid}/activities", async (
                Guid? projectId,
                Guid? taskId,
                int? page,
                int? pageSize,
                IListActivitiesUseCase list,
                CancellationToken cancellationToken) =>
            Results.Ok(await list.ExecuteAsync(
                projectId,
                taskId,
                new PageRequest(page ?? 1, pageSize ?? DefaultPageSize),
                cancellationToken)))
            .WithTags("Activity")
            .RequireAuthorization(WorkBoardClaims.WorkspaceMemberPolicy)
            .WithName("ListActivities")
            .WithSummary("Change history, workspace-wide or filtered to a project or task.")
            .Produces<PagedResult<ActivityDto>>();

        return app;
    }
}
