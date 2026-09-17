using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Workspaces.UpdateWorkspace;

/// <inheritdoc cref="IUpdateWorkspaceUseCase"/>
public sealed class UpdateWorkspaceUseCase(IWorkspaceRepository workspaces, IUnitOfWork unitOfWork)
    : IUpdateWorkspaceUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(
        Guid workspaceId,
        UpdateWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var workspace = await workspaces.FindForUpdateAsync(workspaceId, cancellationToken)
                        ?? throw new NotFoundException("Workspace", workspaceId);

        workspace.Rename(request.Name);
        workspace.Describe(request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
