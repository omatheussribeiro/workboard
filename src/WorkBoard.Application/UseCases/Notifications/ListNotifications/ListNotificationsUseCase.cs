using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Notifications;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Notifications.ListNotifications;

/// <inheritdoc cref="IListNotificationsUseCase"/>
/// <remarks>
/// The tenant filter already confines these rows to one workspace; the recipient clause is what
/// stops one member reading another member's feed <em>inside</em> that workspace. Both are needed,
/// and they defend against different things.
/// </remarks>
public sealed class ListNotificationsUseCase(INotificationQueries queries, ICurrentUser currentUser)
    : IListNotificationsUseCase
{
    /// <inheritdoc/>
    public async Task<PagedResult<NotificationDto>> ExecuteAsync(
        bool unreadOnly,
        PageRequest page,
        CancellationToken cancellationToken) =>
        await queries.ListAsync(currentUser.RequireUserId(), unreadOnly, page, cancellationToken);
}
