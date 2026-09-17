namespace WorkBoard.Application.DTOs.Dashboard;

/// <summary>One day of the throughput chart.</summary>
/// <param name="Date">The day.</param>
/// <param name="Completed">Cards completed on it; zero for days with none.</param>
public sealed record ThroughputPointDto(DateOnly Date, int Completed);
