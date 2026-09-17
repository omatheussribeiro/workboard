using WorkBoard.Application.DTOs.Common;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Tasks;

/// <summary>The full card, as the detail panel renders it.</summary>
/// <param name="Id">Card identifier.</param>
/// <param name="Key">Human-readable key, such as <c>WB-42</c>.</param>
/// <param name="ProjectId">Owning project.</param>
/// <param name="ProjectName">Owning project name.</param>
/// <param name="BoardColumnId">Current column.</param>
/// <param name="ColumnName">Current column name.</param>
/// <param name="Category">Status category the current column maps to.</param>
/// <param name="Title">Card title.</param>
/// <param name="Description">Card body.</param>
/// <param name="Priority">Card priority.</param>
/// <param name="DueAtUtc">Deadline, if set.</param>
/// <param name="IsOverdue">Computed server-side so every client agrees on "late".</param>
/// <param name="StartedAtUtc">When the card first entered an in-progress column.</param>
/// <param name="CompletedAtUtc">When the card first entered a done column.</param>
/// <param name="ParentTaskId">Parent card, when this is a subtask.</param>
/// <param name="ParentTaskTitle">Parent card title, for the breadcrumb.</param>
/// <param name="CreatedById">Author identifier.</param>
/// <param name="CreatedByName">Author display name.</param>
/// <param name="CreatedAtUtc">Creation timestamp.</param>
/// <param name="UpdatedAtUtc">Last modification timestamp.</param>
/// <param name="Assignees">Assigned members.</param>
/// <param name="Labels">Applied labels.</param>
/// <param name="Subtasks">Child cards, ordered by number.</param>
/// <param name="RowVersion">Concurrency token the client must send back on the next write.</param>
public sealed record TaskDetailDto(
    Guid Id,
    string Key,
    Guid ProjectId,
    string ProjectName,
    Guid BoardColumnId,
    string ColumnName,
    TaskStatusCategory Category,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTimeOffset? DueAtUtc,
    bool IsOverdue,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    Guid? ParentTaskId,
    string? ParentTaskTitle,
    Guid CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<AssigneeDto> Assignees,
    IReadOnlyList<LabelChipDto> Labels,
    IReadOnlyList<SubtaskDto> Subtasks,
    byte[] RowVersion);
