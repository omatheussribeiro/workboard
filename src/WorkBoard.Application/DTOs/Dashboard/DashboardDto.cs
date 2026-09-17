namespace WorkBoard.Application.DTOs.Dashboard;

/// <summary>Everything the workspace dashboard renders, in one response.</summary>
/// <param name="Totals">Headline counters.</param>
/// <param name="ByStatus">Card counts per status category.</param>
/// <param name="ByPriority">Card counts per priority.</param>
/// <param name="Projects">Per-project completion progress.</param>
/// <param name="Throughput">Completions per day over the trailing window, gaps included.</param>
/// <param name="MyOpenTasks">The caller's most urgent open cards.</param>
public sealed record DashboardDto(
    WorkspaceTotalsDto Totals,
    IReadOnlyList<StatusSliceDto> ByStatus,
    IReadOnlyList<PrioritySliceDto> ByPriority,
    IReadOnlyList<ProjectProgressDto> Projects,
    IReadOnlyList<ThroughputPointDto> Throughput,
    IReadOnlyList<MyTaskDto> MyOpenTasks);
