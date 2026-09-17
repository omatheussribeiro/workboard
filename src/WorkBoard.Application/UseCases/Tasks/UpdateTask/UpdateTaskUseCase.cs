using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.UseCases.Tasks.GetTask;

namespace WorkBoard.Application.UseCases.Tasks.UpdateTask;

/// <inheritdoc cref="IUpdateTaskUseCase"/>
public sealed class UpdateTaskUseCase(
    ITaskRepository tasks,
    IGetTaskUseCase getTask,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IUpdateTaskUseCase
{
    /// <inheritdoc/>
    public async Task<TaskDetailDto> ExecuteAsync(
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUser.RequireUserId();

        var task = await tasks.FindForUpdateAsync(taskId, cancellationToken)
                   ?? throw new NotFoundException("Task", taskId);

        // Telling EF the value the client last saw makes the UPDATE carry
        // "WHERE RowVersion = @original". If someone else has written since, zero rows match and the
        // unit of work turns the resulting failure into a 409.
        tasks.SetOriginalRowVersion(task, request.RowVersion);

        task.ChangeTitle(request.Title, actorId);
        task.ChangeDescription(request.Description);
        task.ChangePriority(request.Priority, actorId);
        task.SetDeadline(request.DueAtUtc, clock.UtcNow, actorId);

        await unitOfWork.SaveChangesWithConcurrencyCheckAsync(ConflictMessages.StaleTask, cancellationToken);
        return await getTask.ExecuteAsync(taskId, cancellationToken);
    }
}
