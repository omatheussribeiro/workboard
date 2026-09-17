using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Notifications;
using WorkBoard.Application.UseCases.Notifications.CountUnreadNotifications;
using WorkBoard.Application.UseCases.Notifications.ListNotifications;
using WorkBoard.Application.UseCases.Notifications.MarkAllNotificationsRead;
using WorkBoard.Application.UseCases.Notifications.MarkNotificationRead;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Endpoints;

/// <summary>In-app notifications for the calling user.</summary>
public static class NotificationEndpoints
{
    private const int DefaultPageSize = 20;

    /// <summary>Maps the notification routes.</summary>
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces/{workspaceId:guid}/notifications")
            .WithTags("Notifications")
            .RequireAuthorization(WorkBoardClaims.WorkspaceMemberPolicy);

        group.MapGet("/", async (
                bool? unreadOnly,
                int? page,
                int? pageSize,
                IListNotificationsUseCase list,
                CancellationToken cancellationToken) =>
            Results.Ok(await list.ExecuteAsync(
                unreadOnly ?? false,
                new PageRequest(page ?? 1, pageSize ?? DefaultPageSize),
                cancellationToken)))
            .WithName("ListNotifications")
            .Produces<PagedResult<NotificationDto>>();

        group.MapGet("/unread-count", async (
                ICountUnreadNotificationsUseCase countUnread,
                CancellationToken cancellationToken) =>
            Results.Ok(new { count = await countUnread.ExecuteAsync(cancellationToken) }))
            .WithName("CountUnreadNotifications");

        group.MapPost("/{notificationId:guid}/read", async (
                Guid notificationId,
                IMarkNotificationReadUseCase markRead,
                CancellationToken cancellationToken) =>
            {
                await markRead.ExecuteAsync(notificationId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("MarkNotificationRead")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/read-all", async (
                IMarkAllNotificationsReadUseCase markAllRead,
                CancellationToken cancellationToken) =>
            Results.Ok(new { updated = await markAllRead.ExecuteAsync(cancellationToken) }))
            .WithName("MarkAllNotificationsRead");

        return app;
    }
}
