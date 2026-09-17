using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when someone joins a workspace.</summary>
/// <param name="WorkspaceId">The workspace.</param>
/// <param name="UserId">The new member.</param>
/// <param name="Role">The role they were given.</param>
public sealed record WorkspaceMemberAdded(Guid WorkspaceId, Guid UserId, WorkspaceRole Role) : IDomainEvent;
