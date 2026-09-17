using WorkBoard.Application.DTOs.Labels;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Services;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.UseCases.Labels.CreateLabel;

/// <inheritdoc cref="ICreateLabelUseCase"/>
/// <remarks>
/// The duplicate check is here for a readable message; the unique index on
/// <c>(WorkspaceId, Name)</c> is what actually enforces it, because two requests can pass this check
/// simultaneously and only one can win the insert.
/// </remarks>
public sealed class CreateLabelUseCase(
    ILabelRepository labels,
    ILabelNameGuard nameGuard,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICreateLabelUseCase
{
    /// <inheritdoc/>
    public async Task<LabelDto> ExecuteAsync(CreateLabelRequest request, CancellationToken cancellationToken)
    {
        var workspaceId = tenant.RequireWorkspaceId();
        var name = request.Name.Trim();

        await nameGuard.EnsureFreeAsync(name, exceptId: null, cancellationToken);

        var label = Label.Create(workspaceId, name, request.ColorHex);
        labels.Add(label);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LabelDto(label.Id, label.Name, label.ColorHex, TaskCount: 0);
    }
}
