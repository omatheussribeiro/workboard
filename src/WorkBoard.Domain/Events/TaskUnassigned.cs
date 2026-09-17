using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when an assignment is removed.</summary>
/// <param name="TaskId">The card.</param>
/// <param name="WorkspaceId">Owning workspace.</param>
/// <param name="AssigneeId">Who was unassigned.</param>
public sealed record TaskUnassigned(Guid TaskId, Guid WorkspaceId, Guid AssigneeId) : IDomainEvent;
