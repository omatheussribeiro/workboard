using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// Join entity between <see cref="User"/> and <see cref="Workspace"/>, carrying the role.
/// </summary>
/// <remarks>
/// Modelled as an entity rather than a skip navigation because the relationship has data of its own
/// (role, join date) and its own lifecycle. The primary key is the composite
/// <c>(WorkspaceId, UserId)</c>: it makes "is this user in this workspace" a clustered-index seek,
/// which is the hottest read in the system — every authenticated request performs it.
/// </remarks>
public sealed class WorkspaceMember : IWorkspaceScoped
{
    private WorkspaceMember() { }

    private WorkspaceMember(Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid WorkspaceId { get; private set; }

    public Workspace Workspace { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public WorkspaceRole Role { get; private set; }

    public DateTimeOffset JoinedAtUtc { get; private set; }

    internal static WorkspaceMember Create(Guid workspaceId, Guid userId, WorkspaceRole role) =>
        new(Guard.NotDefault(workspaceId), Guard.NotDefault(userId), role);

    internal void ChangeRole(WorkspaceRole role) => Role = role;
}
