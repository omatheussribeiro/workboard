using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when a card is assigned to someone. Produces both an activity entry and a notification.</summary>
/// <param name="TaskId">The card.</param>
/// <param name="WorkspaceId">Owning workspace.</param>
/// <param name="ProjectId">Owning project.</param>
/// <param name="AssigneeId">Who it was assigned to.</param>
/// <param name="ActorId">Who assigned it.</param>
/// <param name="Title">Card title, so the notification can be written without a second query.</param>
public sealed record TaskAssigned(
    Guid TaskId,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid AssigneeId,
    Guid ActorId,
    string Title) : IDomainEvent;
