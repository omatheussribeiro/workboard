using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when a card is opened.</summary>
/// <param name="TaskId">The new card.</param>
/// <param name="WorkspaceId">Owning workspace.</param>
/// <param name="ProjectId">Owning project.</param>
/// <param name="Title">Title at creation.</param>
/// <param name="ActorId">Who created it.</param>
public sealed record TaskCreated(Guid TaskId, Guid WorkspaceId, Guid ProjectId, string Title, Guid ActorId)
    : IDomainEvent;
