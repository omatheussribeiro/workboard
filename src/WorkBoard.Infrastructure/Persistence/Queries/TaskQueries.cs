using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

public sealed class TaskQueries(WorkBoardDbContext context) : ITaskQueries
{
    public Task<TaskDetailDto?> GetAsync(Guid taskId, DateTimeOffset nowUtc, CancellationToken cancellationToken) =>
        context.Tasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(TaskMappings.ToDetail(context, nowUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<TaskListItemDto>> ListAsync(
        TaskFilter filter,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var query = ApplySort(ApplyFilter(context.Tasks.AsNoTracking(), filter, nowUtc), filter.Sort);

        return await query
            .Select(TaskMappings.ToListItem(nowUtc))
            .ToPagedResultAsync(filter.Page, cancellationToken);
    }

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
