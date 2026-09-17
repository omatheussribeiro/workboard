using WorkBoard.Application.DTOs.Dashboard;
using WorkBoard.Application.UseCases.Dashboard.GetDashboard;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Endpoints;

/// <summary>The workspace dashboard.</summary>
public static class DashboardEndpoints
{
    /// <summary>Maps the dashboard route.</summary>
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/{workspaceId:guid}/dashboard", async (
                IGetDashboardUseCase get,
                CancellationToken cancellationToken) =>
            Results.Ok(await get.ExecuteAsync(cancellationToken)))
            .WithTags("Dashboard")
            .RequireAuthorization(WorkBoardClaims.WorkspaceMemberPolicy)
            .WithName("GetDashboard")
            .WithSummary("Workspace totals, status and priority breakdowns, throughput and the caller's open tasks.")
            .Produces<DashboardDto>();

        return app;
    }
}
