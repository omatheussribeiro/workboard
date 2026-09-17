namespace WorkBoard.Application.UseCases.Notifications.MarkNotificationRead;

/// <summary>Acknowledges one notification.</summary>
public interface IMarkNotificationReadUseCase
{
    /// <summary>Marks it read. A notification belonging to someone else is reported as not found.</summary>
    Task ExecuteAsync(Guid notificationId, CancellationToken cancellationToken);
}
