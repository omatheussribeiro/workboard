using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Boards;

/// <summary>Adds a column to the end of a board.</summary>
/// <param name="Name">Column heading; unique within the board.</param>
/// <param name="Category">Status category cards in it are considered to be in.</param>
/// <param name="WipLimit">Optional work-in-progress limit.</param>
public sealed record CreateColumnRequest(string Name, TaskStatusCategory Category, int? WipLimit);
