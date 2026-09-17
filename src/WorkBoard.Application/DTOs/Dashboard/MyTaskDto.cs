using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Dashboard;

/// <summary>One of the caller's open cards, as listed on the dashboard.</summary>
/// <param name="Id">Card identifier.</param>
/// <param name="Key">Human-readable key.</param>
/// <param name="Title">Card title.</param>
/// <param name="Priority">Card priority.</param>
/// <param name="DueAtUtc">Deadline, if set.</param>
/// <param name="IsOverdue">Whether the deadline has passed.</param>
/// <param name="ColumnName">Column the card sits in.</param>
public sealed record MyTaskDto(
    Guid Id,
    string Key,
    string Title,
    TaskPriority Priority,
    DateTimeOffset? DueAtUtc,
    bool IsOverdue,
    string ColumnName);
