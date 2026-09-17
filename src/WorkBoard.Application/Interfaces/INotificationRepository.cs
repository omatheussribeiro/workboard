using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>Write-side access to a user's notifications.</summary>
public interface INotificationRepository
{
    /// <summary>Loads one notification tracked, scoped to its recipient.</summary>
    Task<Notification?> FindForRecipientAsync(Guid notificationId, Guid recipientId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks every unread notification of a recipient as read in one statement.
    /// </summary>
    /// <remarks>
    /// Implemented with <c>ExecuteUpdateAsync</c>: this is a set-based update over rows nobody needs
    /// in memory, and loading a few hundred entities to flip one boolean on each would be the
    /// textbook misuse of an ORM. It bypasses the change tracker, so it has to be the last word on
    /// those rows within the request.
    /// </remarks>
    Task<int> MarkAllReadAsync(Guid recipientId, DateTimeOffset readAtUtc, CancellationToken cancellationToken);
}
