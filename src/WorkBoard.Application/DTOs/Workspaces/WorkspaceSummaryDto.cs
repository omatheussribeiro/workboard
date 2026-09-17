using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Workspaces;

/// <summary>A workspace as it appears in the switcher and on the settings page.</summary>
/// <param name="Id">Workspace identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Slug">URL segment.</param>
/// <param name="Description">Optional blurb.</param>
/// <param name="MyRole">The calling user's role here, which drives what the UI offers.</param>
/// <param name="MemberCount">Number of members.</param>
/// <param name="ProjectCount">Number of projects that are not archived.</param>
/// <param name="CreatedAtUtc">Creation timestamp.</param>
public sealed record WorkspaceSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    WorkspaceRole MyRole,
    int MemberCount,
    int ProjectCount,
    DateTimeOffset CreatedAtUtc);
