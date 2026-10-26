using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Notifications;

namespace WorkBoard.Application.Interfaces;

public interface INotificationQueries
{
    Task<int> CountUnreadAsync(Guid recipientId, CancellationToken cancellationToken);

    Task<PagedResult<NotificationDto>> ListAsync(
        Guid recipientId,
        bool unreadOnly,
        PageRequest page,
        CancellationToken cancellationToken);
}
