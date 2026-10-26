using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Workspaces.ListMyWorkspaces;

/// <inheritdoc cref="IListMyWorkspacesUseCase"/>
/// <remarks>
/// One of only two reads in the application that legitimately cross tenants, and the reason is
/// structural: "which workspaces does this user belong to" cannot be filtered by a workspace that
/// has not been chosen yet. It is anchored on the caller's own user id, which is what makes crossing
/// the filter safe, and it is covered by an isolation test.
/// </remarks>
public sealed class ListMyWorkspacesUseCase(IWorkspaceQueries queries, ICurrentUser currentUser)
    : IListMyWorkspacesUseCase
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorkspaceSummaryDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        return await queries.ListForUserAsync(userId, cancellationToken);
    }
}
