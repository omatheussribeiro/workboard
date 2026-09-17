using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Tasks;

/// <summary>A subtask as listed inside its parent, without the parent's full detail.</summary>
/// <param name="Id">Subtask identifier.</param>
/// <param name="Key">Human-readable key, such as <c>WB-42</c>.</param>
/// <param name="Title">Subtask title.</param>
/// <param name="IsCompleted">Whether it has reached a done column.</param>
/// <param name="Priority">Subtask priority.</param>
public sealed record SubtaskDto(Guid Id, string Key, string Title, bool IsCompleted, TaskPriority Priority);
