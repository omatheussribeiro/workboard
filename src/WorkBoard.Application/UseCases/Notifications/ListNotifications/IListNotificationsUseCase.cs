using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Notifications;

namespace WorkBoard.Application.UseCases.Notifications.ListNotifications;

/// <summary>Reads the caller's notifications in the current workspace.</summary>
public interface IListNotificationsUseCase
{
    /// <summary>One page of notifications, newest first.</summary>
    Task<PagedResult<NotificationDto>> ExecuteAsync(
        bool unreadOnly,
        PageRequest page,
        CancellationToken cancellationToken);
}
