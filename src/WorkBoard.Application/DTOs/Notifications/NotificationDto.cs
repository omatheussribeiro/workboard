using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Notifications;

/// <summary>One in-app notification.</summary>
/// <param name="Id">Notification identifier.</param>
/// <param name="Type">What happened.</param>
/// <param name="Message">Rendered sentence, built when the notification was written.</param>
/// <param name="TaskId">Card to open when the notification is clicked, if any.</param>
/// <param name="IsRead">Whether it has been acknowledged.</param>
/// <param name="CreatedAtUtc">When it was raised.</param>
public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Message,
    Guid? TaskId,
    bool IsRead,
    DateTimeOffset CreatedAtUtc);
