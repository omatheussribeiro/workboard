using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when a card changes column.</summary>
/// <param name="TaskId">The moved card.</param>
/// <param name="WorkspaceId">Owning workspace.</param>
/// <param name="ProjectId">Owning project.</param>
/// <param name="FromColumnId">Column it left.</param>
/// <param name="ToColumnId">Column it entered.</param>
/// <param name="ToCategory">Status category of the new column, which is what drives cycle time.</param>
/// <param name="ActorId">Who moved it.</param>
public sealed record TaskMoved(
    Guid TaskId,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid FromColumnId,
    Guid ToColumnId,
    TaskStatusCategory ToCategory,
    Guid ActorId) : IDomainEvent;
