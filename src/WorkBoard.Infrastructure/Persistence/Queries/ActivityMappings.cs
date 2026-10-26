using System.Linq.Expressions;
using WorkBoard.Application.DTOs.Activities;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

internal static class ActivityMappings
{
    public static Expression<Func<Activity, ActivityDto>> ToDto(WorkBoardDbContext context) =>
        activity => new ActivityDto(
            activity.Id,
            activity.Type,
            activity.Summary,
            activity.ActorId,
            context.Users.Where(user => user.Id == activity.ActorId).Select(user => user.DisplayName).First(),
            activity.ProjectId,
            activity.TaskId,
            activity.PayloadJson,
            activity.OccurredAtUtc);
}
