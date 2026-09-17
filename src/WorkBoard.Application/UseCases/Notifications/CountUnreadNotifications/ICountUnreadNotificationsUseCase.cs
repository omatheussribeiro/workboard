namespace WorkBoard.Application.UseCases.Notifications.CountUnreadNotifications;

/// <summary>The unread count behind the bell badge.</summary>
public interface ICountUnreadNotificationsUseCase
{
    /// <summary>Counts the caller's unread notifications in this workspace.</summary>
    Task<int> ExecuteAsync(CancellationToken cancellationToken);
}
