using WorkBoard.Application.DTOs.Activities;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Activities.ListActivities;

/// <inheritdoc cref="IListActivitiesUseCase"/>
/// <remarks>
/// Read-only by construction: the table is append-only and nothing in this class writes to it.
/// Entries are produced by the domain-event dispatcher inside the same transaction as the change
/// they describe, which is what makes the history impossible to diverge from the data.
/// </remarks>
public sealed class ListActivitiesUseCase(IActivityQueries queries) : IListActivitiesUseCase
{
    /// <inheritdoc/>
    public async Task<PagedResult<ActivityDto>> ExecuteAsync(
        Guid? projectId,
        Guid? taskId,
        PageRequest page,
        CancellationToken cancellationToken) =>
        await queries.ListAsync(projectId, taskId, page, cancellationToken);
}
