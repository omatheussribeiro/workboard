
namespace WorkBoard.Domain.Interfaces;

/// <summary>
/// Something that happened in the domain, worth recording or reacting to.
/// Collected on the aggregate and dispatched by the persistence layer inside the same
/// transaction as the state change, so an activity entry can never disagree with the data.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc => DateTimeOffset.UtcNow;
}
