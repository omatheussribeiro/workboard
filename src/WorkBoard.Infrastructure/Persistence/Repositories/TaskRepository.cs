using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="ITaskRepository"/>
/// <remarks>
/// Every query runs through the tenant-filtered <c>DbSet</c>, so "not in this workspace" and "does
/// not exist" are the same outcome here — the repository cannot see across tenants even if a caller
/// asks it to.
/// </remarks>
public sealed class TaskRepository(WorkBoardDbContext context) : ITaskRepository
{
    /// <inheritdoc/>
    public Task<WorkItem?> FindAsync(Guid taskId, CancellationToken cancellationToken) =>
        context.Tasks.FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// The three <c>Include</c>s are not decoration. <c>Assign</c>, <c>ApplyLabel</c> and the
    /// subtask rules all inspect the collections before mutating them, so loading the root alone
    /// would have the aggregate decide "this label is not applied yet" against an empty list and
    /// insert a duplicate.
    /// </remarks>
    public Task<WorkItem?> FindForUpdateAsync(Guid taskId, CancellationToken cancellationToken) =>
        context.Tasks
            .Include(task => task.Assignments)
            .Include(task => task.Labels)
            .Include(task => task.ParentTask)
            .FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);

    /// <inheritdoc/>
    public Task<WorkItem?> FindWithSubtasksAsync(Guid taskId, CancellationToken cancellationToken) =>
        context.Tasks
            .Include(task => task.Subtasks)
            .FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);

    /// <inheritdoc/>
    public void SetOriginalRowVersion(WorkItem task, byte[] rowVersion) =>
        context.Entry(task).Property(entity => entity.RowVersion).OriginalValue = rowVersion;

    /// <inheritdoc/>
    public Task<double?> GetLastPositionAsync(Guid columnId, CancellationToken cancellationToken) =>
        context.Tasks
            .Where(task => task.BoardColumnId == columnId)
            .MaxAsync(task => (double?)task.Position, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Guid, double>> GetPositionsAsync(
        Guid columnId,
        IReadOnlyCollection<Guid> taskIds,
        CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
        {
            return new Dictionary<Guid, double>();
        }

        var ids = taskIds as Guid[] ?? [.. taskIds];

        return await context.Tasks
            .AsNoTracking()
            .Where(task => ids.Contains(task.Id) && task.BoardColumnId == columnId)
            .Select(task => new { task.Id, task.Position })
            .ToDictionaryAsync(row => row.Id, row => row.Position, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkItem>> ListColumnForRebalanceAsync(
        Guid columnId,
        CancellationToken cancellationToken) =>
        await context.Tasks
            .Where(task => task.BoardColumnId == columnId)
            .OrderBy(task => task.Position)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public void Add(WorkItem task) => context.Tasks.Add(task);

    /// <inheritdoc/>
    public void Remove(WorkItem task) => context.Tasks.Remove(task);
}
