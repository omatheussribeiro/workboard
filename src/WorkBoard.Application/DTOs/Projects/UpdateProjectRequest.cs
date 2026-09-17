namespace WorkBoard.Application.DTOs.Projects;

/// <summary>The editable fields of a project. The key is not among them: it is baked into every task key.</summary>
/// <param name="Name">New display name.</param>
/// <param name="Description">New blurb, or <c>null</c> to clear it.</param>
public sealed record UpdateProjectRequest(string Name, string? Description);
