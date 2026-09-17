using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Services;

/// <inheritdoc cref="ITaskRelationsService"/>
public sealed class TaskRelationsService(ILabelRepository labels, IWorkspaceRepository workspaces)
    : ITaskRelationsService
{
    /// <inheritdoc/>
    public async Task EnsureMembersAsync(IReadOnlyList<Guid> userIds, CancellationToken cancellationToken)
    {
        if (!await workspaces.AreAllMembersAsync(userIds.Distinct().ToArray(), cancellationToken))
        {
            throw new ConflictException("One or more users are not members of this workspace.");
        }
    }

    /// <inheritdoc/>
    public async Task ApplyAssigneesAsync(
        WorkItem task,
        IReadOnlyList<Guid>? assigneeIds,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (assigneeIds is not { Count: > 0 })
        {
            return;
        }

        await EnsureMembersAsync(assigneeIds, cancellationToken);

        foreach (var userId in assigneeIds.Distinct())
        {
            task.Assign(userId, actorId);
        }
    }

    /// <inheritdoc/>
    public async Task ApplyLabelsAsync(
        WorkItem task,
        IReadOnlyList<Guid>? labelIds,
        CancellationToken cancellationToken)
    {
        if (labelIds is not { Count: > 0 })
        {
            return;
        }

        var requested = labelIds.Distinct().ToArray();
        var found = await labels.ListByIdsAsync(requested, cancellationToken);

        // Loaded through the tenant-filtered set, so a label from another workspace simply does not
        // come back — and is reported as missing rather than as forbidden.
        if (found.Count != requested.Length)
        {
            var missing = requested.Except(found.Select(label => label.Id));
            throw new NotFoundException("Label", string.Join(", ", missing));
        }

        foreach (var label in found)
        {
            task.ApplyLabel(label);
        }
    }
}
