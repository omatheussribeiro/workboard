using Microsoft.EntityFrameworkCore;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Infrastructure.Services;

/// <summary>
/// Reacts to a domain event by adding rows to the unit of work that is about to be saved.
/// </summary>
/// <remarks>
/// <para>
/// Listeners receive the <see cref="DbContext"/> rather than resolving their own. They are running
/// inside <c>SaveChangesAsync</c>, and anything they add has to join the same transaction — a
/// second context would be a second transaction, able to commit while the change that caused it
/// rolls back.
/// </para>
/// <para>
/// Named "listener" rather than the more obvious "handler" because CA1711 reserves the
/// <c>EventHandler</c> suffix for delegates, and a reader who sees that suffix on an interface would
/// reasonably expect the .NET event pattern. These are domain events, which are not that.
/// </para>
/// </remarks>
public interface IDomainEventListener
{
    /// <summary>Reacts to one event, staging its writes into <paramref name="context"/>.</summary>
    /// <param name="domainEvent">The event that was raised.</param>
    /// <param name="context">The context mid-save; writes here join the same transaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(IDomainEvent domainEvent, DbContext context, CancellationToken cancellationToken);
}
