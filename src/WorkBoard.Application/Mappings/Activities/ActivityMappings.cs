using System.Linq.Expressions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.DTOs.Activities;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Mappings.Activities;

/// <summary>Entity-to-DTO projection for the activity feed.</summary>
public static class ActivityMappings
{
    /// <summary>Projects an activity row, resolving the actor's name with a subquery.</summary>
    /// <param name="db">Used for the actor lookup: activities record an id, not a navigation.</param>
    public static Expression<Func<Activity, ActivityDto>> ToDto(IWorkBoardDbContext db) =>
        activity => new ActivityDto(
            activity.Id,
            activity.Type,
            activity.Summary,
            activity.ActorId,
            db.Users.Where(user => user.Id == activity.ActorId).Select(user => user.DisplayName).First(),
            activity.ProjectId,
            activity.TaskId,
            activity.PayloadJson,
            activity.OccurredAtUtc);
}
