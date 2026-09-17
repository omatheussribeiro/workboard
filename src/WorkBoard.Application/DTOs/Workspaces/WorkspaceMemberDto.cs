using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Workspaces;

/// <summary>One row of the member list.</summary>
/// <param name="UserId">Member identifier.</param>
/// <param name="Email">Member address.</param>
/// <param name="DisplayName">Member name.</param>
/// <param name="Role">Role in this workspace.</param>
/// <param name="JoinedAtUtc">When they were added.</param>
public sealed record WorkspaceMemberDto(
    Guid UserId,
    string Email,
    string DisplayName,
    WorkspaceRole Role,
    DateTimeOffset JoinedAtUtc);
