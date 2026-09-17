namespace WorkBoard.Application.DTOs.Projects;

/// <summary>A project as it appears in the sidebar and the project list.</summary>
/// <param name="Id">Project identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Key">Task-key prefix.</param>
/// <param name="Description">Optional blurb.</param>
/// <param name="IsArchived">Archived projects are hidden unless explicitly requested.</param>
/// <param name="TaskCount">Total cards.</param>
/// <param name="OpenTaskCount">Cards that have not reached a done column.</param>
/// <param name="DefaultBoardId">Board the project opens on, or <c>null</c> if it somehow has none.</param>
/// <param name="CreatedAtUtc">Creation timestamp.</param>
public sealed record ProjectSummaryDto(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    bool IsArchived,
    int TaskCount,
    int OpenTaskCount,
    Guid? DefaultBoardId,
    DateTimeOffset CreatedAtUtc);
