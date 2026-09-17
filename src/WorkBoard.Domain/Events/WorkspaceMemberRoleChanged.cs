using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when a member's role changes.</summary>
/// <param name="WorkspaceId">The workspace.</param>
/// <param name="UserId">The member.</param>
/// <param name="Role">Their new role.</param>
public sealed record WorkspaceMemberRoleChanged(Guid WorkspaceId, Guid UserId, WorkspaceRole Role) : IDomainEvent;
