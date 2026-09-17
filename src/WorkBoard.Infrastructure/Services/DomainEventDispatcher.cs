using Microsoft.EntityFrameworkCore;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Services;

/// <summary>
/// Collects domain events off the tracked entities and hands them to the registered listeners.
/// </summary>
/// <remarks>
/// <para>
/// Runs immediately before the database write, inside <see cref="WorkBoardDbContext.SaveChangesAsync"/>.
/// An outbox would be the right answer if these events left the process — they do not: both handlers
/// write to tables in this same database, so an outbox would add a delivery mechanism, a background
/// worker and eventual consistency to solve a problem the transaction already solves. If
/// notifications ever become emails, the outbox goes here. See ADR-011.
/// </para>
/// <para>
/// Listeners may themselves mutate entities that raise further events, so the queue is drained until
/// empty rather than processed in a single pass. The iteration cap turns an accidental event cycle
/// into a clear exception instead of a hang.
/// </para>
/// </remarks>
public sealed class DomainEventDispatcher(IEnumerable<IDomainEventListener> listeners)
{
    private const int MaxPasses = 10;

    public async Task DispatchAsync(DbContext context, CancellationToken cancellationToken)
    {
        var listenerList = listeners.ToList();
        if (listenerList.Count == 0)
        {
            return;
        }

        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var entities = context.ChangeTracker
                .Entries<Entity>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            if (entities.Count == 0)
            {
                return;
            }

            // Cleared before dispatch, not after: a handler that touches the same aggregate would
            // otherwise see the event it is currently handling and reprocess it on the next pass.
            var events = entities.SelectMany(e => e.DomainEvents).ToList();
            foreach (var entity in entities)
            {
                entity.ClearDomainEvents();
            }

            foreach (var domainEvent in events)
            {
                foreach (var listener in listenerList)
                {
                    await listener.HandleAsync(domainEvent, context, cancellationToken);
                }
            }
        }

        throw new InvalidOperationException(
            $"Domain event dispatch did not settle after {MaxPasses} passes; a listener is probably raising the event it handles.");
    }
}
