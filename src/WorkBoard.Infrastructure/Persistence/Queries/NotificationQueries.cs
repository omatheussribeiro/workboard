using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Notifications;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Notifications;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

public sealed class NotificationQueries(WorkBoardDbContext context) : INotificationQueries
{
    public async Task<int> CountUnreadAsync(Guid recipientId, CancellationToken cancellationToken) =>
        await context.Notifications
            .AsNoTracking()
            .CountAsync(
                notification => notification.RecipientId == recipientId && !notification.IsRead,
                cancellationToken);

    public async Task<PagedResult<NotificationDto>> ListAsync(
        Guid recipientId,
        bool unreadOnly,
        PageRequest page,
        CancellationToken cancellationToken) =>
        await context.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientId == recipientId)
            .WhereIf(unreadOnly, notification => !notification.IsRead)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenBy(notification => notification.Id)
            .Select(NotificationMappings.ToDto())
            .ToPagedResultAsync(page, cancellationToken);
}
