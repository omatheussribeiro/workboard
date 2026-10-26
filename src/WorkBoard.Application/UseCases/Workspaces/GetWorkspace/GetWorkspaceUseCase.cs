using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Workspaces.GetWorkspace;

/// <inheritdoc cref="IGetWorkspaceUseCase"/>
/// <remarks>
/// Crosses the tenant filter for the same structural reason as
/// <c>ListMyWorkspacesUseCase</c>, and is anchored on the caller's own user id in the same way.
/// </remarks>
public sealed class GetWorkspaceUseCase(IWorkspaceQueries queries, ICurrentUser currentUser) : IGetWorkspaceUseCase
{
    /// <inheritdoc/>
    public async Task<WorkspaceSummaryDto> ExecuteAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        return await queries.GetForUserAsync(workspaceId, userId, cancellationToken)

               // A non-member and a non-existent workspace get the same 404. Returning 403 for one
               // and 404 for the other would let an outsider probe which workspaces exist.
               ?? throw new NotFoundException("Workspace", workspaceId);
    }
}
