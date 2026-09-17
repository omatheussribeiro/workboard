using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>Write-side access to the workspace aggregate and its membership list.</summary>
public interface IWorkspaceRepository
{
    /// <summary>Loads a workspace tracked with its members.</summary>
    Task<Workspace?> FindForUpdateAsync(Guid workspaceId, CancellationToken cancellationToken);

    /// <summary>
    /// Whether the slug is taken, checked across every tenant.
    /// </summary>
    /// <remarks>
    /// Slugs are global — they appear in URLs — so this has to read past the tenant filter. It is
    /// one of the few places allowed to, and the isolation tests pin the behaviour: the method may
    /// reveal that a slug exists, never anything about the workspace that owns it.
    /// </remarks>
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    /// <summary>Whether every listed user is a member of the current workspace.</summary>
    Task<bool> AreAllMembersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>Stages a new workspace.</summary>
    void Add(Workspace workspace);
}
