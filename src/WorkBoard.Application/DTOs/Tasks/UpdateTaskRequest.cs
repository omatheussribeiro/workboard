using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Tasks;

/// <summary>The editable fields of a card, plus the concurrency token the editor started from.</summary>
/// <param name="Title">New title.</param>
/// <param name="Description">New body, or <c>null</c> to clear it.</param>
/// <param name="Priority">New priority.</param>
/// <param name="DueAtUtc">New deadline, or <c>null</c> to clear it.</param>
/// <param name="RowVersion">The <c>rowversion</c> the client last read. A stale value produces 409.</param>
public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTimeOffset? DueAtUtc,
    byte[] RowVersion);
