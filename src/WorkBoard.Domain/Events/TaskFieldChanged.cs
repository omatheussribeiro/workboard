using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>
/// Raised when one field of a card changes.
/// </summary>
/// <remarks>
/// One event for every scalar field rather than one per field name. The activity feed renders "X
/// changed the priority from High to Low" from the same three strings whichever field it was, so a
/// <c>TaskPriorityChanged</c>, <c>TaskTitleChanged</c> and <c>TaskDeadlineChanged</c> would be three
/// records and three handler branches producing identical output.
/// </remarks>
/// <param name="TaskId">The card.</param>
/// <param name="WorkspaceId">Owning workspace.</param>
/// <param name="Field">Name of the field that changed.</param>
/// <param name="OldValue">Previous value, rendered as text.</param>
/// <param name="NewValue">New value, rendered as text.</param>
/// <param name="ActorId">Who changed it.</param>
public sealed record TaskFieldChanged(
    Guid TaskId,
    Guid WorkspaceId,
    string Field,
    string? OldValue,
    string? NewValue,
    Guid ActorId) : IDomainEvent;
