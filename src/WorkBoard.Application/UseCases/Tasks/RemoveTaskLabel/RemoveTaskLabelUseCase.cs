using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Tasks.RemoveTaskLabel;

/// <inheritdoc cref="IRemoveTaskLabelUseCase"/>
public sealed class RemoveTaskLabelUseCase(ITaskRepository tasks, IUnitOfWork unitOfWork)
    : IRemoveTaskLabelUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid taskId, Guid labelId, CancellationToken cancellationToken)
    {
        var task = await tasks.FindForUpdateAsync(taskId, cancellationToken)
                   ?? throw new NotFoundException("Task", taskId);

        task.RemoveLabel(labelId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
