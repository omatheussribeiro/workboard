using WorkBoard.Application.DTOs.Activities;
using WorkBoard.Application.DTOs.Common;

namespace WorkBoard.Application.UseCases.Activities.ListActivities;

/// <summary>Reads the change history, as a workspace feed or filtered to one project or card.</summary>
public interface IListActivitiesUseCase
{
    /// <summary>One page of the feed, newest first.</summary>
    Task<PagedResult<ActivityDto>> ExecuteAsync(
        Guid? projectId,
        Guid? taskId,
        PageRequest page,
        CancellationToken cancellationToken);
}
