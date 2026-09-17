using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Activities;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.Extensions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Activities;

namespace WorkBoard.Application.UseCases.Activities.ListActivities;

/// <inheritdoc cref="IListActivitiesUseCase"/>
/// <remarks>
/// Read-only by construction: the table is append-only and nothing in this class writes to it.
/// Entries are produced by the domain-event dispatcher inside the same transaction as the change
/// they describe, which is what makes the history impossible to diverge from the data.
/// </remarks>
public sealed class ListActivitiesUseCase(IWorkBoardDbContext db) : IListActivitiesUseCase
{
    /// <inheritdoc/>
    public async Task<PagedResult<ActivityDto>> ExecuteAsync(
        Guid? projectId,
        Guid? taskId,
        PageRequest page,
        CancellationToken cancellationToken) =>
        await db.Activities
            .AsNoTracking()
            .WhereIf(projectId is not null, activity => activity.ProjectId == projectId)
            .WhereIf(taskId is not null, activity => activity.TaskId == taskId)

            // Newest first, with the id as a tiebreaker so paging is deterministic when two
            // activities land in the same millisecond. Ordering by id alone would not work: SQL
            // Server sorts uniqueidentifier by its trailing bytes, so even a time-ordered UUIDv7
            // does not sort chronologically in the database.
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .ThenByDescending(activity => activity.Id)
            .Select(ActivityMappings.ToDto(db))
            .ToPagedResultAsync(page, cancellationToken);
}
