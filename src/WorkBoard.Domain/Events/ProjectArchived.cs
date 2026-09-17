using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when a project is archived.</summary>
/// <param name="ProjectId">The archived project.</param>
/// <param name="WorkspaceId">Owning workspace.</param>
public sealed record ProjectArchived(Guid ProjectId, Guid WorkspaceId) : IDomainEvent;
