namespace WorkBoard.Application.DTOs.Dashboard;

/// <summary>One project's progress bar.</summary>
/// <param name="ProjectId">Project identifier.</param>
/// <param name="Name">Project name.</param>
/// <param name="Key">Project key.</param>
/// <param name="Total">Top-level cards.</param>
/// <param name="Done">Top-level cards that have reached a done column.</param>
public sealed record ProjectProgressDto(Guid ProjectId, string Name, string Key, int Total, int Done);
