namespace WorkBoard.Application.DTOs.Common;

/// <summary>
/// A user shown as the assignee of something.
/// </summary>
/// <remarks>
/// Shared by the task list and the task detail rather than defined once per feature. Three features
/// previously carried byte-identical copies of this record, which meant a change to what an avatar
/// needs had to be made — and reviewed — three times.
/// </remarks>
/// <param name="UserId">Assigned member.</param>
/// <param name="DisplayName">Name shown under the avatar.</param>
/// <param name="Email">Used to build the avatar and the mail link.</param>
public sealed record AssigneeDto(Guid UserId, string DisplayName, string Email);
