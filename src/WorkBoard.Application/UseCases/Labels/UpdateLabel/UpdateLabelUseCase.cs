using WorkBoard.Application.DTOs.Labels;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Services;

namespace WorkBoard.Application.UseCases.Labels.UpdateLabel;

/// <inheritdoc cref="IUpdateLabelUseCase"/>
/// <remarks>
/// The rename is checked against the same unique constraint as a create. Before, it was not:
/// renaming a label onto an existing name reached the index and surfaced as a 500 instead of the
/// 409 the same collision produces at creation.
/// </remarks>
public sealed class UpdateLabelUseCase(
    ILabelRepository labels,
    ILabelNameGuard nameGuard,
    IUnitOfWork unitOfWork) : IUpdateLabelUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid labelId, UpdateLabelRequest request, CancellationToken cancellationToken)
    {
        var label = await labels.FindForUpdateAsync(labelId, cancellationToken)
                    ?? throw new NotFoundException("Label", labelId);

        var name = request.Name.Trim();
        await nameGuard.EnsureFreeAsync(name, labelId, cancellationToken);

        label.Rename(name);
        label.Recolor(request.ColorHex);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
