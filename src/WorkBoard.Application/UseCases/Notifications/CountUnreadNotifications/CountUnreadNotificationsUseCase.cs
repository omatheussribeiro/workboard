using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Notifications.CountUnreadNotifications;

/// <inheritdoc cref="ICountUnreadNotificationsUseCase"/>
public sealed class CountUnreadNotificationsUseCase(IWorkBoardDbContext db, ICurrentUser currentUser)
    : ICountUnreadNotificationsUseCase
{
    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken) =>
        await db.Notifications
            .AsNoTracking()
            .CountAsync(
                notification => notification.RecipientId == currentUser.RequireUserId() && !notification.IsRead,
                cancellationToken);
}
