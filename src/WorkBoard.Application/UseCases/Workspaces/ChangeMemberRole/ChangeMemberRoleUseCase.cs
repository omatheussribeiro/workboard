using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Workspaces.ChangeMemberRole;

/// <inheritdoc cref="IChangeMemberRoleUseCase"/>
public sealed class ChangeMemberRoleUseCase(IWorkspaceRepository workspaces, IUnitOfWork unitOfWork)
    : IChangeMemberRoleUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(
        Guid workspaceId,
        Guid userId,
        ChangeMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var workspace = await workspaces.FindForUpdateAsync(workspaceId, cancellationToken)
                        ?? throw new NotFoundException("Workspace", workspaceId);

        // The aggregate owns the rules about the owner's role and about the last remaining admin,
        // and it has the member list loaded to enforce them.
        workspace.ChangeMemberRole(userId, request.Role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
