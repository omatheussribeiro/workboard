using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Labels.DeleteLabel;

/// <inheritdoc cref="IDeleteLabelUseCase"/>
/// <remarks>
/// The join rows go with it, via a cascade configured on <c>TaskLabel</c>: removing a label should
/// untag the cards, not be blocked by them.
/// </remarks>
public sealed class DeleteLabelUseCase(ILabelRepository labels, IUnitOfWork unitOfWork) : IDeleteLabelUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid labelId, CancellationToken cancellationToken)
    {
        var label = await labels.FindForUpdateAsync(labelId, cancellationToken)
                    ?? throw new NotFoundException("Label", labelId);

        labels.Remove(label);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
