using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Tasks.ApplyTaskLabel;

/// <inheritdoc cref="IApplyTaskLabelUseCase"/>
/// <remarks>
/// The card is loaded with its labels, because the aggregate checks whether the label is already
/// applied before adding it. Loading the root alone would have it decide "no" against an empty list
/// and insert a duplicate, which then fails at the unique index as a 500.
/// </remarks>
public sealed class ApplyTaskLabelUseCase(
    ITaskRepository tasks,
    ILabelRepository labels,
    IUnitOfWork unitOfWork) : IApplyTaskLabelUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid taskId, Guid labelId, CancellationToken cancellationToken)
    {
        var task = await tasks.FindForUpdateAsync(taskId, cancellationToken)
                   ?? throw new NotFoundException("Task", taskId);

        var label = await labels.FindForUpdateAsync(labelId, cancellationToken)
                    ?? throw new NotFoundException("Label", labelId);

        task.ApplyLabel(label);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
