using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Dashboard;

/// <summary>One slice of the status chart.</summary>
/// <param name="Category">Status category.</param>
/// <param name="Count">Cards currently in it.</param>
public sealed record StatusSliceDto(TaskStatusCategory Category, int Count);
