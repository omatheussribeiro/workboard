using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Notifications.CountUnreadNotifications;

/// <inheritdoc cref="ICountUnreadNotificationsUseCase"/>
public sealed class CountUnreadNotificationsUseCase(INotificationQueries queries, ICurrentUser currentUser)
    : ICountUnreadNotificationsUseCase
{
    /// <inheritdoc/>
    public Task<int> ExecuteAsync(CancellationToken cancellationToken) =>
        queries.CountUnreadAsync(currentUser.RequireUserId(), cancellationToken);
}
