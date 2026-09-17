using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Tasks.DeleteTask;

/// <inheritdoc cref="IDeleteTaskUseCase"/>
/// <remarks>
/// Refuses rather than cascading. Deleting a parent takes its children with it, and a child can hold
/// work nobody meant to lose — so the caller is made to decide what happens to them.
/// </remarks>
public sealed class DeleteTaskUseCase(ITaskRepository tasks, IUnitOfWork unitOfWork) : IDeleteTaskUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await tasks.FindWithSubtasksAsync(taskId, cancellationToken)
                   ?? throw new NotFoundException("Task", taskId);

        if (task.Subtasks.Count > 0)
        {
            throw new ConflictException("Delete or detach the subtasks first.");
        }

        tasks.Remove(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
