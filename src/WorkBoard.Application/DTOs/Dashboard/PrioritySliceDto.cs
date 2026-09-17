using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Dashboard;

/// <summary>One slice of the priority chart.</summary>
/// <param name="Priority">Priority level.</param>
/// <param name="Count">Cards at that level.</param>
public sealed record PrioritySliceDto(TaskPriority Priority, int Count);
