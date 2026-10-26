using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Dashboard;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

public sealed class DashboardQueries(WorkBoardDbContext context) : IDashboardQueries
{
    private const int ThroughputWindowDays = 14;
    private const int MyTaskLimit = 10;

    public async Task<DashboardDto> GetAsync(
        Guid currentUserId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        new(
            await GetTotalsAsync(nowUtc, cancellationToken),
            await GetByStatusAsync(cancellationToken),
            await GetByPriorityAsync(cancellationToken),
            await GetProjectProgressAsync(cancellationToken),
            await GetThroughputAsync(nowUtc, cancellationToken),
            await GetMyOpenTasksAsync(currentUserId, nowUtc, cancellationToken));

    private IQueryable<WorkItem> RootTasks() =>
        context.Tasks.AsNoTracking().Where(task => task.ParentTaskId == null);

    private async Task<WorkspaceTotalsDto> GetTotalsAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken) =>
        new(
            Projects: await context.Projects.CountAsync(project => !project.IsArchived, cancellationToken),
            Tasks: await RootTasks().CountAsync(cancellationToken),
            OpenTasks: await RootTasks().CountAsync(task => task.CompletedAtUtc == null, cancellationToken),
            OverdueTasks: await RootTasks().CountAsync(
                task => task.CompletedAtUtc == null && task.DueAtUtc != null && task.DueAtUtc < nowUtc,
                cancellationToken),
            Members: await context.WorkspaceMembers.CountAsync(cancellationToken));

    private async Task<IReadOnlyList<StatusSliceDto>> GetByStatusAsync(CancellationToken cancellationToken) =>
        await RootTasks()
            .GroupBy(task => task.BoardColumn.Category)
            .Select(group => new StatusSliceDto(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<PrioritySliceDto>> GetByPriorityAsync(CancellationToken cancellationToken) =>
        await RootTasks()
            .GroupBy(task => task.Priority)
            .Select(group => new PrioritySliceDto(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<ProjectProgressDto>> GetProjectProgressAsync(CancellationToken cancellationToken) =>
        await context.Projects
            .AsNoTracking()
            .Where(project => !project.IsArchived)
            .OrderBy(project => project.Name)
            .Select(project => new ProjectProgressDto(
                project.Id,
                project.Name,
                project.Key,
                project.Tasks.Count(task => task.ParentTaskId == null),
                project.Tasks.Count(task => task.ParentTaskId == null && task.CompletedAtUtc != null)))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<ThroughputPointDto>> GetThroughputAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var since = nowUtc.AddDays(-ThroughputWindowDays);

        var completions = await RootTasks()
            .Where(task => task.CompletedAtUtc != null && task.CompletedAtUtc >= since)
            .GroupBy(task => task.CompletedAtUtc!.Value.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var byDate = completions.ToDictionary(entry => DateOnly.FromDateTime(entry.Date), entry => entry.Count);

        return Enumerable
            .Range(0, ThroughputWindowDays)
            .Select(offset => DateOnly.FromDateTime(nowUtc.AddDays(offset - ThroughputWindowDays + 1).UtcDateTime))
            .Select(date => new ThroughputPointDto(date, byDate.GetValueOrDefault(date)))
            .ToList();
    }

    private async Task<IReadOnlyList<MyTaskDto>> GetMyOpenTasksAsync(
        Guid userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken) =>
        await RootTasks()
            .Where(task => task.CompletedAtUtc == null
                           && task.Assignments.Any(assignment => assignment.UserId == userId))
            .OrderBy(task => task.DueAtUtc == null)
            .ThenBy(task => task.DueAtUtc)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Id)
            .Take(MyTaskLimit)
            .Select(task => new MyTaskDto(
                task.Id,
                task.Project.Key + "-" + task.Number,
                task.Title,
                task.Priority,
                task.DueAtUtc,
                task.DueAtUtc != null && task.DueAtUtc < nowUtc,
                task.BoardColumn.Name))
            .ToListAsync(cancellationToken);
}
