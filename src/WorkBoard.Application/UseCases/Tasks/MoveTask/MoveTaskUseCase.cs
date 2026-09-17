using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Services;
using WorkBoard.Application.UseCases.Tasks.GetTask;

namespace WorkBoard.Application.UseCases.Tasks.MoveTask;

/// <inheritdoc cref="IMoveTaskUseCase"/>
/// <remarks>
/// The two neighbour positions are read in one query rather than two, and both columns are loaded
/// tracked because the aggregate adjusts their WIP counters. When the drop is inside the same column
/// the same tracked instance is used for both — EF resolves it from the identity map, so there is no
/// second round trip and no risk of two objects representing one row.
/// </remarks>
public sealed class MoveTaskUseCase(
    ITaskRepository tasks,
    IBoardRepository boards,
    IColumnRebalancer rebalancer,
    IGetTaskUseCase getTask,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IMoveTaskUseCase
{
    /// <inheritdoc/>
    public async Task<TaskDetailDto> ExecuteAsync(
        Guid taskId,
        MoveTaskRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUser.RequireUserId();

        var task = await tasks.FindForUpdateAsync(taskId, cancellationToken)
                   ?? throw new NotFoundException("Task", taskId);

        tasks.SetOriginalRowVersion(task, request.RowVersion);

        var source = await boards.FindColumnAsync(task.BoardColumnId, cancellationToken)
                     ?? throw new NotFoundException("Board column", task.BoardColumnId);

        var target = request.TargetColumnId == source.Id
            ? source
            : await boards.FindColumnAsync(request.TargetColumnId, cancellationToken)
              ?? throw new NotFoundException("Board column", request.TargetColumnId);

        var neighbourIds = new[] { request.PreviousTaskId, request.NextTaskId }
            .OfType<Guid>()
            .ToArray();

        var positions = await tasks.GetPositionsAsync(target.Id, neighbourIds, cancellationToken);

        task.MoveTo(
            target,
            source,
            PositionOf(request.PreviousTaskId),
            PositionOf(request.NextTaskId),
            clock.UtcNow,
            actorId);

        await unitOfWork.SaveChangesWithConcurrencyCheckAsync(ConflictMessages.StaleTask, cancellationToken);

        if (task.NeedsRebalance)
        {
            await rebalancer.RebalanceAsync(target.Id, cancellationToken);
        }

        return await getTask.ExecuteAsync(taskId, cancellationToken);

        double? PositionOf(Guid? id) =>
            id is { } value && positions.TryGetValue(value, out var position) ? position : null;
    }
}
