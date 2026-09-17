using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Events;
using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// The tenant. Every other piece of content hangs off exactly one workspace, and the whole
/// authorisation model is "which workspaces is this user a member of, and with what role".
/// </summary>
public sealed class Workspace : Entity, IAuditable
{
    private readonly List<WorkspaceMember> _members = [];
    private readonly List<Project> _projects = [];

    private Workspace() { }

    private Workspace(Guid id, string name, string slug, Guid ownerId)
        : base(id)
    {
        Name = name;
        Slug = slug;
        OwnerId = ownerId;
    }

    public string Name { get; private set; } = null!;

    /// <summary>URL-safe identifier, unique across the platform.</summary>
    public string Slug { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid OwnerId { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<WorkspaceMember> Members => _members;

    public IReadOnlyCollection<Project> Projects => _projects;

    public static Workspace Create(string name, string slug, Guid ownerId)
    {
        var workspace = new Workspace(
            Guid.CreateVersion7(),
            Guard.MaxLength(Guard.NotEmpty(name), 120),
            NormalizeSlug(slug),
            Guard.NotDefault(ownerId));

        // The creator is seeded as Owner in the same unit of work. A workspace with no owner would
        // be unreachable: nobody could grant themselves access to it afterwards.
        workspace._members.Add(WorkspaceMember.Create(workspace.Id, ownerId, WorkspaceRole.Owner));
        workspace.Raise(new WorkspaceCreated(workspace.Id, ownerId, workspace.Name));
        return workspace;
    }

    public void Rename(string name) => Name = Guard.MaxLength(Guard.NotEmpty(name), 120);

    public void Describe(string? description) =>
        Description = description is null ? null : Guard.MaxLength(description, 1000);

    public void Archive() => IsArchived = true;

    public void Restore() => IsArchived = false;

    public WorkspaceMember AddMember(Guid userId, WorkspaceRole role)
    {
        Guard.NotDefault(userId);
        Guard.Against(role == WorkspaceRole.Owner, "A workspace has exactly one owner; transfer ownership instead.");
        Guard.Against(_members.Any(m => m.UserId == userId), "User is already a member of this workspace.");

        var member = WorkspaceMember.Create(Id, userId, role);
        _members.Add(member);
        Raise(new WorkspaceMemberAdded(Id, userId, role));
        return member;
    }

    public void ChangeMemberRole(Guid userId, WorkspaceRole role)
    {
        var member = RequireMember(userId);
        Guard.Against(member.Role == WorkspaceRole.Owner, "The workspace owner cannot be demoted.");
        Guard.Against(role == WorkspaceRole.Owner, "Use TransferOwnership to move ownership.");

        member.ChangeRole(role);
        Raise(new WorkspaceMemberRoleChanged(Id, userId, role));
    }

    public void RemoveMember(Guid userId)
    {
        var member = RequireMember(userId);
        Guard.Against(member.Role == WorkspaceRole.Owner, "The workspace owner cannot be removed.");

        _members.Remove(member);
        Raise(new WorkspaceMemberRemoved(Id, userId));
    }

    public void TransferOwnership(Guid newOwnerId)
    {
        var next = RequireMember(newOwnerId);
        var current = _members.Single(m => m.Role == WorkspaceRole.Owner);
        if (current.UserId == newOwnerId)
        {
            return;
        }

        current.ChangeRole(WorkspaceRole.Admin);
        next.ChangeRole(WorkspaceRole.Owner);
        OwnerId = newOwnerId;
    }

    /// <summary>
    /// The single question the authorisation layer asks. Kept on the aggregate rather than in a
    /// service so the rule is unit-testable without a database.
    /// </summary>
    public bool HasAtLeast(Guid userId, WorkspaceRole role) =>
        _members.Any(m => m.UserId == userId && m.Role >= role);

    void IAuditable.SetCreated(DateTimeOffset at) => CreatedAtUtc = at;

    void IAuditable.SetUpdated(DateTimeOffset at) => UpdatedAtUtc = at;

    private WorkspaceMember RequireMember(Guid userId) =>
        _members.SingleOrDefault(m => m.UserId == userId)
        ?? throw new DomainException("User is not a member of this workspace.");

    private static string NormalizeSlug(string slug)
    {
        var value = Guard.MaxLength(Guard.NotEmpty(slug), 60).ToLowerInvariant();
        Guard.Against(
            !value.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'),
            "Slug may only contain lowercase letters, digits and hyphens.");
        Guard.Against(value.StartsWith('-') || value.EndsWith('-'), "Slug cannot start or end with a hyphen.");
        return value;
    }
}
