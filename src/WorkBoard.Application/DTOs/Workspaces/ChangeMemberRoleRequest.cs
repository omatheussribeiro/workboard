using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Workspaces;

/// <summary>Changes one member's role.</summary>
/// <param name="Role">The new role.</param>
public sealed record ChangeMemberRoleRequest(WorkspaceRole Role);
