using WorkBoard.Api.Extensions;
using WorkBoard.Application.DTOs.Search;
using WorkBoard.Application.UseCases.Search.SearchWorkspace;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Endpoints;

/// <summary>The command-palette search.</summary>
public static class SearchEndpoints
{
    /// <summary>Maps the search route.</summary>
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/{workspaceId:guid}/search", async (
                string q,
                ISearchWorkspaceUseCase search,
                CancellationToken cancellationToken) =>
            Results.Ok(await search.ExecuteAsync(q, cancellationToken)))
            .WithTags("Search")
            .RequireAuthorization(WorkBoardClaims.WorkspaceMemberPolicy)

            // Search is cheap per call and easy to call in a loop from a keystroke handler, so it
            // gets its own limiter rather than sharing the general one.
            .RequireRateLimiting(RateLimitPolicies.Search)
            .WithName("Search")
            .Produces<SearchResultsDto>();

        return app;
    }
}
