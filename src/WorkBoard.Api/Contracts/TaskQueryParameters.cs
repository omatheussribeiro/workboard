using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Api.Contracts;

/// <summary>
/// Query-string shape of <see cref="TaskFilter"/>.
/// </summary>
/// <remarks>
/// A separate type from the filter the application layer consumes. The two look alike today, and
/// keeping them apart means the query string is a transport concern that can grow aliases and
/// defaults without those leaking into the domain-facing contract. Bound with
/// <c>[AsParameters]</c>, so each field appears in OpenAPI as a documented, typed parameter rather
/// than as an opaque object.
/// </remarks>
/// <param name="ProjectId">Restrict to one project.</param>
/// <param name="ColumnId">Restrict to one column.</param>
/// <param name="Category">Restrict to one status category.</param>
/// <param name="Priority">Restrict to one priority.</param>
/// <param name="AssigneeId">Only cards assigned to this member.</param>
/// <param name="LabelId">Only cards carrying this label.</param>
/// <param name="Overdue">Only cards past their deadline and not yet done.</param>
/// <param name="IncludeSubtasks">Include subtasks, which are hidden by default.</param>
/// <param name="Search">Free text matched against title and description.</param>
/// <param name="Sort">Ordering.</param>
/// <param name="Page">One-based page number.</param>
/// <param name="PageSize">Rows per page, clamped server-side.</param>
public sealed record TaskQueryParameters(
    Guid? ProjectId,
    Guid? ColumnId,
    TaskStatusCategory? Category,
    TaskPriority? Priority,
    Guid? AssigneeId,
    Guid? LabelId,
    bool? Overdue,
    bool? IncludeSubtasks,
    string? Search,
    TaskSort? Sort,
    int? Page,
    int? PageSize)
{
    /// <summary>Converts the query string into the application-layer filter.</summary>
    public TaskFilter ToFilter() => new(
        ProjectId,
        ColumnId,
        Category,
        Priority,
        AssigneeId,
        LabelId,
        Overdue,
        IncludeSubtasks ?? false,
        Search,
        Sort ?? TaskSort.Newest)
    {
        // PageRequest clamps both values, so a caller asking for page -3 with 10,000 rows gets a
        // sane page rather than an error or a table scan.
        Page = new PageRequest(Page ?? 1, PageSize ?? 25),
    };
}
