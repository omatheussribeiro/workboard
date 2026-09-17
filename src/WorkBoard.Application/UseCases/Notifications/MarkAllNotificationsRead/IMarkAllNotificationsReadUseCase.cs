namespace WorkBoard.Application.UseCases.Notifications.MarkAllNotificationsRead;

/// <summary>Clears the badge in one statement.</summary>
public interface IMarkAllNotificationsReadUseCase
{
    /// <summary>Marks every unread notification read and returns how many were affected.</summary>
    Task<int> ExecuteAsync(CancellationToken cancellationToken);
}
