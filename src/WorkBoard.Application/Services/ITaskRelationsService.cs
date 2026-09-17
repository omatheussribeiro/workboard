using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Services;

/// <summary>
/// The assignee and label rules shared by the task use cases.
/// </summary>
/// <remarks>
/// Extracted because creating a card and assigning to an existing one need the identical membership
/// check, and because that check is a security control rather than a convenience — see
/// <see cref="EnsureMembersAsync"/>. A rule that matters this much is worth having in one place with
/// one test.
/// </remarks>
public interface ITaskRelationsService
{
    /// <summary>
    /// Rejects users who are not members of the current workspace.
    /// </summary>
    /// <remarks>
    /// The tenant filter stops the caller <em>reading</em> other workspaces, but assignment writes a
    /// user id, and a query filter does not constrain what an insert may reference. Without this
    /// check a caller could attach an arbitrary user id to a card and see that person's name
    /// rendered on it.
    /// </remarks>
    Task EnsureMembersAsync(IReadOnlyList<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>Assigns every listed member, after checking they belong to the workspace.</summary>
    Task ApplyAssigneesAsync(
        WorkItem task,
        IReadOnlyList<Guid>? assigneeIds,
        Guid actorId,
        CancellationToken cancellationToken);

    /// <summary>Applies every listed label, failing if any of them does not exist here.</summary>
    Task ApplyLabelsAsync(WorkItem task, IReadOnlyList<Guid>? labelIds, CancellationToken cancellationToken);
}
