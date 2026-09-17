using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.Tests;

/// <summary>A stand-in for the authenticated principal, with no HTTP anywhere near it.</summary>
internal sealed class TestCurrentUser(Guid userId, WorkspaceRole? role = WorkspaceRole.Member) : ICurrentUser
{
    /// <inheritdoc/>
    public Guid? UserId { get; } = userId;

    /// <inheritdoc/>
    public bool IsAuthenticated => UserId is not null;

    /// <inheritdoc/>
    public bool IsSystemAdmin { get; init; }

    /// <inheritdoc/>
    public WorkspaceRole? WorkspaceRole { get; } = role;

    /// <inheritdoc/>
    public Guid RequireUserId() => UserId ?? throw new InvalidOperationException("Not authenticated.");
}
