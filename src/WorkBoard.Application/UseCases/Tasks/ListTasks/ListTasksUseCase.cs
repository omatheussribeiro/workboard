using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Extensions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Tasks;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.UseCases.Tasks.ListTasks;

/// <inheritdoc cref="IListTasksUseCase"/>
public sealed class ListTasksUseCase(IWorkBoardDbContext db, IClock clock) : IListTasksUseCase
{
    /// <inheritdoc/>
    public async Task<PagedResult<TaskListItemDto>> ExecuteAsync(
        TaskFilter filter,
        CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;

        var query = ApplySort(ApplyFilter(db.Tasks.AsNoTracking(), filter, nowUtc), filter.Sort);

        return await query
            .Select(TaskMappings.ToListItem(nowUtc))
            .ToPagedResultAsync(filter.Page, cancellationToken);
    }

    /// <summary>
    /// Narrows the query to the requested slice.
    /// </summary>
    /// <remarks>
    /// Free-text search is the only clause that cannot seek an index: <c>Contains</c> becomes
    /// <c>LIKE '%term%'</c>, which is a scan. It is acceptable because the tenant filter has already
    /// reduced the set to one workspace, and because the alternative — a SQL Server full-text
    /// catalog — adds infrastructure to provision and keep in sync for a feature used a handful of
    /// times a day. If search ever becomes hot, this is the single place full-text goes.
    /// </remarks>
    private static IQueryable<WorkItem> ApplyFilter(
        IQueryable<WorkItem> query,
        TaskFilter filter,
        DateTimeOffset nowUtc)
    {
        query = query
            .WhereIf(filter.ProjectId is not null, task => task.ProjectId == filter.ProjectId)
            .WhereIf(filter.BoardColumnId is not null, task => task.BoardColumnId == filter.BoardColumnId)
            .WhereIf(filter.Category is not null, task => task.BoardColumn.Category == filter.Category)
            .WhereIf(filter.Priority is not null, task => task.Priority == filter.Priority)
            .WhereIf(
                filter.AssigneeId is not null,
                task => task.Assignments.Any(assignment => assignment.UserId == filter.AssigneeId))
            .WhereIf(filter.LabelId is not null, task => task.Labels.Any(applied => applied.LabelId == filter.LabelId))
            .WhereIf(
                filter.OverdueOnly == true,
                task => task.DueAtUtc != null && task.CompletedAtUtc == null && task.DueAtUtc < nowUtc)
            .WhereIf(!filter.IncludeSubtasks, task => task.ParentTaskId == null);

        if (string.IsNullOrWhiteSpace(filter.Search))
        {
            return query;
        }

        var term = filter.Search.Trim();
        return query.Where(task =>
            task.Title.Contains(term) || (task.Description != null && task.Description.Contains(term)));
    }

    /// <summary>
    /// Applies the requested ordering.
    /// </summary>
    /// <remarks>
    /// Every branch ends in a unique tiebreaker. Without one, OFFSET/FETCH is free to return the
    /// same row on two consecutive pages whenever the sort key ties — which it does constantly on
    /// priority — and the client silently loses rows.
    /// </remarks>
    private static IQueryable<WorkItem> ApplySort(IQueryable<WorkItem> query, TaskSort sort) => sort switch
    {
        TaskSort.Oldest => query.OrderBy(task => task.CreatedAtUtc).ThenBy(task => task.Id),
        TaskSort.DueDate => query
            .OrderBy(task => task.DueAtUtc == null)
            .ThenBy(task => task.DueAtUtc)
            .ThenBy(task => task.Id),
        TaskSort.Priority => query
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.DueAtUtc)
            .ThenBy(task => task.Id),
        _ => query.OrderByDescending(task => task.CreatedAtUtc).ThenBy(task => task.Id),
    };
}
