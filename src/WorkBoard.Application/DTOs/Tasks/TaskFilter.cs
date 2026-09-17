using WorkBoard.Application.DTOs.Common;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Tasks;

/// <summary>
/// The filter behind the task list and the search box.
/// </summary>
/// <remarks>
/// Every field is optional and applied with <c>WhereIf</c>, so unused filters contribute no SQL at
/// all — the generated statement stays as narrow as the request. Applying them unconditionally with
/// <c>x == null || ...</c> would produce an OR-heavy predicate that SQL Server cannot index-seek,
/// and would give every caller the same bad plan through parameter sniffing.
/// </remarks>
/// <param name="ProjectId">Restrict to one project.</param>
/// <param name="BoardColumnId">Restrict to one column.</param>
/// <param name="Category">Restrict to one status category.</param>
/// <param name="Priority">Restrict to one priority.</param>
/// <param name="AssigneeId">Only cards assigned to this member.</param>
/// <param name="LabelId">Only cards carrying this label.</param>
/// <param name="OverdueOnly">Only cards past their deadline and not yet done.</param>
/// <param name="IncludeSubtasks">Subtasks are hidden by default so a list is not doubled.</param>
/// <param name="Search">Free text matched against title and description.</param>
/// <param name="Sort">Ordering; always completed by a unique tiebreaker.</param>
public sealed record TaskFilter(
    Guid? ProjectId = null,
    Guid? BoardColumnId = null,
    TaskStatusCategory? Category = null,
    TaskPriority? Priority = null,
    Guid? AssigneeId = null,
    Guid? LabelId = null,
    bool? OverdueOnly = null,
    bool IncludeSubtasks = false,
    string? Search = null,
    TaskSort Sort = TaskSort.Newest)
{
    /// <summary>Page to return; clamped by <see cref="PageRequest"/> itself.</summary>
    public PageRequest Page { get; init; } = new();
}
