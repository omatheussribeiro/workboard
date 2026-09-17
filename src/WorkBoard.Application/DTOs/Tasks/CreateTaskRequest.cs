using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Tasks;

/// <summary>Everything needed to open a new card, including the relations it starts with.</summary>
/// <param name="ProjectId">Project the card belongs to.</param>
/// <param name="BoardColumnId">Column the card is dropped into; must belong to the project.</param>
/// <param name="Title">Short summary shown on the card.</param>
/// <param name="Description">Optional long-form body.</param>
/// <param name="Priority">Initial priority.</param>
/// <param name="DueAtUtc">Optional deadline; rejected when it is already in the past.</param>
/// <param name="ParentTaskId">Set to create the card as a subtask of another.</param>
/// <param name="AssigneeIds">Workspace members to assign, at most ten.</param>
/// <param name="LabelIds">Workspace labels to apply, at most twenty.</param>
public sealed record CreateTaskRequest(
    Guid ProjectId,
    Guid BoardColumnId,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTimeOffset? DueAtUtc,
    Guid? ParentTaskId,
    IReadOnlyList<Guid>? AssigneeIds,
    IReadOnlyList<Guid>? LabelIds);
