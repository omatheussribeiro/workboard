using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Dashboard;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.UseCases.Dashboard.GetDashboard;

/// <inheritdoc cref="IGetDashboardUseCase"/>
/// <remarks>
/// <para>
/// Six aggregates over the same table. The naive version loads the tasks once and computes them in
/// memory, which is fine at fifty cards and ruinous at fifty thousand — it transfers every row,
/// descriptions included, to produce six integers. Each is instead a <c>GroupBy</c>/<c>Count</c>
/// that SQL Server answers from an index.
/// </para>
/// <para>
/// They are issued as separate awaited queries rather than concurrently. A <c>DbContext</c> is not
/// thread-safe: running them under <c>Task.WhenAll</c> on one context throws at best and corrupts
/// the change tracker at worst. Sequential round trips cost single-digit milliseconds each, which is
/// the right trade for this endpoint.
/// </para>
/// <para>
/// <see cref="ExecuteAsync"/> reads as the six sections it returns; each section is a private method
/// below. That split is the only thing separating this from a seventy-line method whose parts could
/// only be understood by reading all of it.
/// </para>
/// </remarks>
public sealed class GetDashboardUseCase(IWorkBoardDbContext db, ICurrentUser currentUser, IClock clock)
    : IGetDashboardUseCase
{
    private const int ThroughputWindowDays = 14;
    private const int MyTaskLimit = 10;

    /// <inheritdoc/>
    public async Task<DashboardDto> ExecuteAsync(CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var userId = currentUser.RequireUserId();

        return new DashboardDto(
            await GetTotalsAsync(nowUtc, cancellationToken),
            await GetByStatusAsync(cancellationToken),
            await GetByPriorityAsync(cancellationToken),
            await GetProjectProgressAsync(cancellationToken),
            await GetThroughputAsync(nowUtc, cancellationToken),
            await GetMyOpenTasksAsync(userId, nowUtc, cancellationToken));
    }

    /// <summary>
    /// Top-level cards only.
    /// </summary>
    /// <remarks>
    /// Subtasks are excluded from every dashboard figure. Counting them would make a team that
    /// breaks work down look more productive than one that does not, which is the opposite of what
    /// these numbers are for.
    /// </remarks>
    private IQueryable<WorkItem> RootTasks() =>
        db.Tasks.AsNoTracking().Where(task => task.ParentTaskId == null);

    private async Task<WorkspaceTotalsDto> GetTotalsAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken) =>
        new(
            Projects: await db.Projects.CountAsync(project => !project.IsArchived, cancellationToken),
            Tasks: await RootTasks().CountAsync(cancellationToken),
            OpenTasks: await RootTasks().CountAsync(task => task.CompletedAtUtc == null, cancellationToken),
            OverdueTasks: await RootTasks().CountAsync(
                task => task.CompletedAtUtc == null && task.DueAtUtc != null && task.DueAtUtc < nowUtc,
                cancellationToken),
            Members: await db.WorkspaceMembers.CountAsync(cancellationToken));

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
        await db.Projects
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

    /// <summary>
    /// Completions per day over the trailing window.
    /// </summary>
    /// <remarks>
    /// Grouped by date <em>in the database</em>. Projecting the timestamp and bucketing client-side
    /// would pull one row per completed card just to count them. The empty days are filled in
    /// afterwards, because a chart that silently omits days with no completions compresses the gaps
    /// and reads as steady output.
    /// </remarks>
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
