using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Boards;

/// <summary>The editable fields of a column.</summary>
/// <param name="Name">New heading.</param>
/// <param name="Category">New status category.</param>
/// <param name="WipLimit">New limit, or <c>null</c> to remove it.</param>
public sealed record UpdateColumnRequest(string Name, TaskStatusCategory Category, int? WipLimit);
