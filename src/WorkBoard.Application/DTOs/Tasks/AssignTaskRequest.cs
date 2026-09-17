namespace WorkBoard.Application.DTOs.Tasks;

/// <summary>Assigns one workspace member to a card.</summary>
/// <param name="UserId">The member to assign; rejected when they do not belong to the workspace.</param>
public sealed record AssignTaskRequest(Guid UserId);
