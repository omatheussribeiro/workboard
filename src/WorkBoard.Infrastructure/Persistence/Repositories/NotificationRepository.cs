using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="INotificationRepository"/>
public sealed class NotificationRepository(WorkBoardDbContext context) : INotificationRepository
{
    /// <inheritdoc/>
    public Task<Notification?> FindForRecipientAsync(
        Guid notificationId,
        Guid recipientId,
        CancellationToken cancellationToken) =>
        context.Notifications.FirstOrDefaultAsync(
            notification => notification.Id == notificationId && notification.RecipientId == recipientId,
            cancellationToken);

    /// <inheritdoc/>
    public Task<int> MarkAllReadAsync(
        Guid recipientId,
        DateTimeOffset readAtUtc,
        CancellationToken cancellationToken) =>
        context.Notifications
            .Where(notification => notification.RecipientId == recipientId && !notification.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(notification => notification.IsRead, true)
                    .SetProperty(notification => notification.ReadAtUtc, readAtUtc),
                cancellationToken);
}
