namespace WorkBoard.Application.DTOs.Workspaces;

/// <summary>The editable fields of a workspace. The slug is not among them, because it is a URL.</summary>
/// <param name="Name">New display name.</param>
/// <param name="Description">New blurb, or <c>null</c> to clear it.</param>
public sealed record UpdateWorkspaceRequest(string Name, string? Description);
