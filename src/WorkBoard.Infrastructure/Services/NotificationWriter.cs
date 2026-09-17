using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Events;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Infrastructure.Services;

/// <summary>
/// Turns domain events into <see cref="Notification"/> rows.
/// </summary>
/// <remarks>
/// Kept separate from <see cref="ActivityWriter"/> although both react to the same events. They
/// answer different questions — "what happened" versus "who needs to know" — and only one of them
/// will grow an email or push transport later. Splitting them now means that change touches one
/// file.
/// </remarks>
public sealed class NotificationWriter(IClock clock) : IDomainEventListener
{
    public async Task HandleAsync(IDomainEvent domainEvent, DbContext context, CancellationToken cancellationToken)
    {
        var at = clock.UtcNow;

        switch (domainEvent)
        {
            case TaskAssigned e when e.AssigneeId != e.ActorId:
                // Assigning yourself notifies nobody. Telling someone what they just did is noise,
                // and noise is what makes people stop reading notifications.
                context.Add(Notification.Create(
                    e.WorkspaceId, e.AssigneeId, NotificationType.TaskAssigned,
                    $"You were assigned to {e.Title}", at, e.TaskId));
                break;

            case TaskCommented e:
                await NotifyWatchersAsync(context, e, at, cancellationToken);
                break;

            case WorkspaceMemberAdded e:
                context.Add(Notification.Create(
                    e.WorkspaceId, e.UserId, NotificationType.AddedToWorkspace,
                    "You were added to a workspace", at));
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Notifies everyone assigned to the task, except whoever wrote the comment.
    /// </summary>
    /// <remarks>
    /// The assignee ids are read with a projection, not by loading the assignment entities: this
    /// runs inside <c>SaveChangesAsync</c>, and materialising entities there would add them to a
    /// change tracker that is mid-flight. A projection reads the ids and tracks nothing.
    /// </remarks>
    private static async Task NotifyWatchersAsync(
        DbContext context,
        TaskCommented e,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        var recipientIds = await context.Set<TaskAssignment>()
            .AsNoTracking()
            .Where(a => a.TaskId == e.TaskId && a.UserId != e.AuthorId)
            .Select(a => a.UserId)
            .ToListAsync(cancellationToken);

        foreach (var recipientId in recipientIds)
        {
            context.Add(Notification.Create(
                e.WorkspaceId, recipientId, NotificationType.TaskCommented,
                $"New comment on {e.Title}", at, e.TaskId));
        }
    }
}
