using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Activities;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

public sealed class ActivityQueries(WorkBoardDbContext context) : IActivityQueries
{
    public async Task<PagedResult<ActivityDto>> ListAsync(
        Guid? projectId,
        Guid? taskId,
        PageRequest page,
        CancellationToken cancellationToken) =>
        await context.Activities
            .AsNoTracking()
            .WhereIf(projectId is not null, activity => activity.ProjectId == projectId)
            .WhereIf(taskId is not null, activity => activity.TaskId == taskId)
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .ThenByDescending(activity => activity.Id)
            .Select(ActivityMappings.ToDto(context))
            .ToPagedResultAsync(page, cancellationToken);
}
