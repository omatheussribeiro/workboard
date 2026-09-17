using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Workspaces.RemoveWorkspaceMember;

/// <inheritdoc cref="IRemoveWorkspaceMemberUseCase"/>
/// <remarks>
/// Takes effect on the removed member's very next request, because membership is read per request
/// rather than carried in their access token. An integration test pins exactly that.
/// </remarks>
public sealed class RemoveWorkspaceMemberUseCase(IWorkspaceRepository workspaces, IUnitOfWork unitOfWork)
    : IRemoveWorkspaceMemberUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var workspace = await workspaces.FindForUpdateAsync(workspaceId, cancellationToken)
                        ?? throw new NotFoundException("Workspace", workspaceId);

        workspace.RemoveMember(userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
