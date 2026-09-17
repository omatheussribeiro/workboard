namespace WorkBoard.Application.DTOs.Dashboard;

/// <summary>The headline counters across the workspace.</summary>
/// <param name="Projects">Projects that are not archived.</param>
/// <param name="Tasks">Top-level cards.</param>
/// <param name="OpenTasks">Cards that have not reached a done column.</param>
/// <param name="OverdueTasks">Open cards past their deadline.</param>
/// <param name="Members">Workspace members.</param>
public sealed record WorkspaceTotalsDto(int Projects, int Tasks, int OpenTasks, int OverdueTasks, int Members);
