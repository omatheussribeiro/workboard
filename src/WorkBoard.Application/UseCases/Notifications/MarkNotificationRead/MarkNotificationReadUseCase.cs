using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Notifications.MarkNotificationRead;

/// <inheritdoc cref="IMarkNotificationReadUseCase"/>
/// <remarks>
/// Notifications are <em>produced</em> by the domain-event dispatcher, never here. That keeps "who
/// gets told about what" next to the event that caused it, rather than spread across every use case
/// that might want to notify someone.
/// </remarks>
public sealed class MarkNotificationReadUseCase(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IMarkNotificationReadUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        var notification = await notifications.FindForRecipientAsync(notificationId, userId, cancellationToken)
                           ?? throw new NotFoundException("Notification", notificationId);

        notification.MarkRead(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
