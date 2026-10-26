using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Tasks.ListTasks;

/// <inheritdoc cref="IListTasksUseCase"/>
public sealed class ListTasksUseCase(ITaskQueries queries, IClock clock) : IListTasksUseCase
{
    /// <inheritdoc/>
    public async Task<PagedResult<TaskListItemDto>> ExecuteAsync(
        TaskFilter filter,
        CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;

        return await queries.ListAsync(filter, nowUtc, cancellationToken);
    }

}
