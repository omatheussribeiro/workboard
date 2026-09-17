using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Workspaces;

/// <summary>Adds an existing account to a workspace.</summary>
/// <param name="Email">Address of an account that already exists.</param>
/// <param name="Role">Role to grant; <c>Owner</c> is rejected, as there is exactly one owner.</param>
public sealed record AddMemberRequest(string Email, WorkspaceRole Role);
