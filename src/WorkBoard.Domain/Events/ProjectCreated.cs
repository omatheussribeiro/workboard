using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when a project is created. The activity writer turns it into a feed entry.</summary>
/// <param name="ProjectId">The new project.</param>
/// <param name="WorkspaceId">Owning workspace.</param>
/// <param name="Name">Project name at creation.</param>
/// <param name="ActorId">Who created it.</param>
public sealed record ProjectCreated(Guid ProjectId, Guid WorkspaceId, string Name, Guid ActorId) : IDomainEvent;
