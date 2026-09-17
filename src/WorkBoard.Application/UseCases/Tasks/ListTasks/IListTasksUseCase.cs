using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Tasks;

namespace WorkBoard.Application.UseCases.Tasks.ListTasks;

/// <summary>The filtered, paged task list.</summary>
public interface IListTasksUseCase
{
    /// <summary>Returns one page of cards matching the filter.</summary>
    Task<PagedResult<TaskListItemDto>> ExecuteAsync(TaskFilter filter, CancellationToken cancellationToken);
}
