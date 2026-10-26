using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Tasks;

namespace WorkBoard.Application.Interfaces;

public interface ITaskQueries
{
    Task<TaskDetailDto?> GetAsync(Guid taskId, DateTimeOffset nowUtc, CancellationToken cancellationToken);

    Task<PagedResult<TaskListItemDto>> ListAsync(
        TaskFilter filter,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);
}
