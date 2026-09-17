namespace WorkBoard.Application.DTOs.Common;

/// <summary>
/// An assignee as rendered on a Kanban card.
/// </summary>
/// <remarks>
/// Narrower than <see cref="AssigneeDto"/>, and separate from it on purpose. A board renders a few
/// hundred cards at once and shows only initials, so the address would be several hundred strings
/// of payload that nothing displays — and it is personal data that does not need to leave the
/// server for this view.
/// </remarks>
/// <param name="UserId">Assigned member.</param>
/// <param name="DisplayName">Name behind the initials, used for the tooltip.</param>
public sealed record BoardAssigneeDto(Guid UserId, string DisplayName);
