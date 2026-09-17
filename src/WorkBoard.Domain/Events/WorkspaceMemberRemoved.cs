using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when a member is removed from a workspace.</summary>
/// <param name="WorkspaceId">The workspace.</param>
/// <param name="UserId">The removed member.</param>
public sealed record WorkspaceMemberRemoved(Guid WorkspaceId, Guid UserId) : IDomainEvent;
