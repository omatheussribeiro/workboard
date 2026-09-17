using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IWorkspaceRepository"/>
public sealed class WorkspaceRepository(WorkBoardDbContext context) : IWorkspaceRepository
{
    /// <inheritdoc/>
    /// <remarks>
    /// With the members loaded, because that is the collection the aggregate's rules run against:
    /// "the owner cannot be removed", "no duplicate members", "the last admin stays". Loading the
    /// root alone would let each of those decide against nothing.
    /// </remarks>
    public Task<Workspace?> FindForUpdateAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        context.Workspaces
            .Include(workspace => workspace.Members)
            .FirstOrDefaultAsync(workspace => workspace.Id == workspaceId, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Reads past the tenant filter, and has to: a slug is unique across the installation, so the
    /// collision being checked for is with a workspace the caller cannot see. The query returns a
    /// boolean and nothing else, which is the whole reason it is safe to ask.
    /// </remarks>
    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        context.Workspaces
            .IgnoreQueryFilters()
            .AnyAsync(workspace => workspace.Slug == slug, cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> AreAllMembersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return true;
        }

        var ids = userIds as Guid[] ?? [.. userIds];

        // Counted rather than fetched: the caller only needs to know whether any id is missing, and
        // the filtered set already restricts the count to the current workspace.
        var found = await context.WorkspaceMembers
            .CountAsync(membership => ids.Contains(membership.UserId), cancellationToken);

        return found == ids.Length;
    }

    /// <inheritdoc/>
    public void Add(Workspace workspace) => context.Workspaces.Add(workspace);
}
