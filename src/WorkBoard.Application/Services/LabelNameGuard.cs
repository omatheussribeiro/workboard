using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.Services;

/// <inheritdoc cref="ILabelNameGuard"/>
public sealed class LabelNameGuard(ILabelRepository labels) : ILabelNameGuard
{
    /// <inheritdoc/>
    public async Task EnsureFreeAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await labels.NameExistsAsync(name, exceptId, cancellationToken))
        {
            throw new ConflictException($"A label named '{name}' already exists in this workspace.");
        }
    }
}
