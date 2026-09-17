using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when a workspace — and therefore a tenant — is created.</summary>
/// <param name="WorkspaceId">The new workspace.</param>
/// <param name="OwnerId">Its owner, who cannot later be removed.</param>
/// <param name="Name">Workspace name at creation.</param>
public sealed record WorkspaceCreated(Guid WorkspaceId, Guid OwnerId, string Name) : IDomainEvent;
