using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// Base for every persisted entity.
/// </summary>
/// <remarks>
/// <para>
/// Identity is a UUIDv7 generated in the domain, not by the database. The aggregate is therefore
/// fully valid, and referenceable, before it ever reaches EF Core — domain events can carry its id,
/// and a whole object graph can be built and asserted on without a round trip.
/// </para>
/// <para>
/// One thing v7 does <em>not</em> buy on SQL Server, contrary to the usual claim: clustered-index
/// locality. SQL Server does not compare <c>uniqueidentifier</c> byte by byte — it compares the last
/// six bytes first — so the timestamp v7 places in the <em>first</em> six bytes is the last thing it
/// looks at. A v7 key is therefore as randomly ordered to SQL Server as <c>Guid.NewGuid()</c>.
/// The tables where that would hurt (Tasks, Activities, Notifications) instead declare a
/// non-clustered primary key and cluster on a genuinely ascending key; see their configurations
/// and ADR-002.
/// </para>
/// </remarks>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(Guid id) => Id = id;

    /// <summary>EF Core materialisation constructor.</summary>
    protected Entity() { }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id && Id != Guid.Empty;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
