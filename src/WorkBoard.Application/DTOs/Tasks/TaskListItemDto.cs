using WorkBoard.Application.DTOs.Common;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Tasks;

/// <summary>A card as it appears in a list or a search result — narrower than the detail on purpose.</summary>
/// <param name="Id">Card identifier.</param>
/// <param name="Key">Human-readable key.</param>
/// <param name="Title">Card title.</param>
/// <param name="ProjectId">Owning project.</param>
/// <param name="ProjectKey">Owning project key.</param>
/// <param name="ColumnName">Current column name.</param>
/// <param name="Category">Status category of the current column.</param>
/// <param name="Priority">Card priority.</param>
/// <param name="DueAtUtc">Deadline, if set.</param>
/// <param name="IsOverdue">Computed server-side.</param>
/// <param name="Assignees">Assigned members.</param>
public sealed record TaskListItemDto(
    Guid Id,
    string Key,
    string Title,
    Guid ProjectId,
    string ProjectKey,
    string ColumnName,
    TaskStatusCategory Category,
    TaskPriority Priority,
    DateTimeOffset? DueAtUtc,
    bool IsOverdue,
    IReadOnlyList<AssigneeDto> Assignees);
