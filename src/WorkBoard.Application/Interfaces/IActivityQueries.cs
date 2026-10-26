using WorkBoard.Application.DTOs.Activities;
using WorkBoard.Application.DTOs.Common;

namespace WorkBoard.Application.Interfaces;

public interface IActivityQueries
{
    Task<PagedResult<ActivityDto>> ListAsync(
        Guid? projectId,
        Guid? taskId,
        PageRequest page,
        CancellationToken cancellationToken);
}
