namespace WorkBoard.Application.DTOs.Workspaces;

/// <summary>A new workspace, which is also a new tenant.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Slug">URL segment; unique across the whole installation.</param>
/// <param name="Description">Optional blurb.</param>
public sealed record CreateWorkspaceRequest(string Name, string Slug, string? Description);
