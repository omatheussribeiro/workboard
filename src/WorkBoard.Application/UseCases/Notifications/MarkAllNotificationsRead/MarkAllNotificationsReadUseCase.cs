using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Notifications.MarkAllNotificationsRead;

/// <inheritdoc cref="IMarkAllNotificationsReadUseCase"/>
/// <remarks>
/// Delegated to a set-based UPDATE that never materialises a row — see
/// <see cref="INotificationRepository.MarkAllReadAsync"/>. The trade-off is worth stating: bulk
/// operations bypass the change tracker, so they raise no domain events and do not refresh anything
/// already loaded in this context. That is acceptable here precisely because marking a notification
/// read carries no domain meaning; it would not be acceptable for a status change.
/// </remarks>
public sealed class MarkAllNotificationsReadUseCase(
    INotificationRepository notifications,
    ICurrentUser currentUser,
    IClock clock) : IMarkAllNotificationsReadUseCase
{
    /// <inheritdoc/>
    public Task<int> ExecuteAsync(CancellationToken cancellationToken) =>
        notifications.MarkAllReadAsync(currentUser.RequireUserId(), clock.UtcNow, cancellationToken);
}
