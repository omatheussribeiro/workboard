namespace WorkBoard.Api.Endpoints;

/// <summary>
/// Maps every endpoint group, in one place.
/// </summary>
/// <remarks>
/// Each group lives in its own file, named after the resource it serves. They used to be collected
/// into a single <c>CollaborationEndpoints</c> file on the argument that each was only a handful of
/// routes; the file reached 229 lines and six unrelated concerns, and "where are the notification
/// routes" stopped having an obvious answer. One file per resource makes that answer the file name.
/// </remarks>
public static class EndpointRegistration
{
    /// <summary>Registers all WorkBoard HTTP endpoints.</summary>
    public static IEndpointRouteBuilder MapWorkBoardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapWorkspaceEndpoints();
        app.MapProjectEndpoints();
        app.MapBoardEndpoints();
        app.MapTaskEndpoints();
        app.MapCommentEndpoints();
        app.MapLabelEndpoints();
        app.MapDashboardEndpoints();
        app.MapNotificationEndpoints();
        app.MapActivityEndpoints();
        app.MapSearchEndpoints();

        return app;
    }
}
