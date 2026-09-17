using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Tasks.UnassignTask;

/// <inheritdoc cref="IUnassignTaskUseCase"/>
public sealed class UnassignTaskUseCase(ITaskRepository tasks, IUnitOfWork unitOfWork) : IUnassignTaskUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid taskId, Guid userId, CancellationToken cancellationToken)
    {
        var task = await tasks.FindForUpdateAsync(taskId, cancellationToken)
                   ?? throw new NotFoundException("Task", taskId);

        task.Unassign(userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
