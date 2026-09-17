using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Notifications;
using WorkBoard.Application.Extensions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Notifications;

namespace WorkBoard.Application.UseCases.Notifications.ListNotifications;

/// <inheritdoc cref="IListNotificationsUseCase"/>
/// <remarks>
/// The tenant filter already confines these rows to one workspace; the recipient clause is what
/// stops one member reading another member's feed <em>inside</em> that workspace. Both are needed,
/// and they defend against different things.
/// </remarks>
public sealed class ListNotificationsUseCase(IWorkBoardDbContext db, ICurrentUser currentUser)
    : IListNotificationsUseCase
{
    /// <inheritdoc/>
    public async Task<PagedResult<NotificationDto>> ExecuteAsync(
        bool unreadOnly,
        PageRequest page,
        CancellationToken cancellationToken) =>
        await db.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientId == currentUser.RequireUserId())
            .WhereIf(unreadOnly, notification => !notification.IsRead)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenBy(notification => notification.Id)
            .Select(NotificationMappings.ToDto())
            .ToPagedResultAsync(page, cancellationToken);
}
