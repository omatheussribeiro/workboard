namespace WorkBoard.Application.DTOs.Projects;

/// <summary>A new project, created together with a default board.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Key">Short prefix for task keys, such as <c>WB</c>; unique within the workspace.</param>
/// <param name="Description">Optional blurb.</param>
public sealed record CreateProjectRequest(string Name, string Key, string? Description);
