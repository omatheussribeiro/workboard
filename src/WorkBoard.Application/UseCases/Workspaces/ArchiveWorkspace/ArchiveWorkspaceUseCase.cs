using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Workspaces.ArchiveWorkspace;

/// <inheritdoc cref="IArchiveWorkspaceUseCase"/>
public sealed class ArchiveWorkspaceUseCase(
    IWorkspaceRepository workspaces,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IArchiveWorkspaceUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var workspace = await workspaces.FindForUpdateAsync(workspaceId, cancellationToken)
                        ?? throw new NotFoundException("Workspace", workspaceId);

        // Checked here rather than by a policy: "admin" is enough for everything else in a
        // workspace, but archiving takes the whole tenant offline for every member, so it is
        // reserved to the one account that cannot itself be removed. A policy only knows roles; it
        // has no way to express "the owner".
        if (workspace.OwnerId != currentUser.RequireUserId())
        {
            throw new ForbiddenException("Only the workspace owner can archive it.");
        }

        workspace.Archive();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
