using System.Linq.Expressions;
using WorkBoard.Application.DTOs.Notifications;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Mappings.Notifications;

/// <summary>Entity-to-DTO projection for notifications.</summary>
public static class NotificationMappings
{
    /// <summary>Projects a notification onto its client representation.</summary>
    public static Expression<Func<Notification, NotificationDto>> ToDto() =>
        notification => new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Message,
            notification.TaskId,
            notification.IsRead,
            notification.CreatedAtUtc);
}
