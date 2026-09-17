using System.Linq.Expressions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Mappings.Tasks;

/// <summary>
/// Entity-to-DTO projections for tasks, as expression trees.
/// </summary>
/// <remarks>
/// <para>
/// Mapping is expressed as <see cref="Expression{TDelegate}"/> rather than as a method that takes a
/// loaded entity, and that difference is the whole point. An expression is handed to EF Core, which
/// translates it into the SELECT list: the database returns exactly the columns the DTO needs, and
/// the related rows arrive in the same round trip. A mapping function over materialised entities
/// would instead force the full row to be loaded, tracked, and then thrown away — and would pull
/// assignees and labels with one extra query per card, which is the N+1 this codebase is written to
/// avoid.
/// </para>
/// <para>
/// It is also why there is no AutoMapper here. A convention-based mapper buys nothing when the
/// source is a query rather than an object, costs a startup configuration step and a runtime
/// validation test, and turns a rename into a failure found at request time instead of at compile
/// time. These projections are longer to read and impossible to get silently wrong. See ADR-013.
/// </para>
/// <para>
/// Each projection is a method rather than a static field because it closes over request state —
/// "now", and the context used for the author lookup. Building it per call costs an expression-tree
/// allocation, which is noise next to the round trip that follows.
/// </para>
/// </remarks>
public static class TaskMappings
{
    /// <summary>Projects a card onto its list representation.</summary>
    /// <param name="nowUtc">Evaluation instant for <c>IsOverdue</c>, so one list cannot disagree with itself.</param>
    public static Expression<Func<WorkItem, TaskListItemDto>> ToListItem(DateTimeOffset nowUtc) =>
        task => new TaskListItemDto(
            task.Id,
            task.Project.Key + "-" + task.Number,
            task.Title,
            task.ProjectId,
            task.Project.Key,
            task.BoardColumn.Name,
            task.BoardColumn.Category,
            task.Priority,
            task.DueAtUtc,
            task.DueAtUtc != null && task.CompletedAtUtc == null && task.DueAtUtc < nowUtc,
            task.Assignments
                .Select(assignment => new AssigneeDto(
                    assignment.UserId,
                    assignment.User.DisplayName,
                    assignment.User.Email))
                .ToList());

    /// <summary>Projects a card onto its full detail representation.</summary>
    /// <param name="db">Used for the author lookup, which has no navigation property to travel.</param>
    /// <param name="nowUtc">Evaluation instant for <c>IsOverdue</c>.</param>
    public static Expression<Func<WorkItem, TaskDetailDto>> ToDetail(IWorkBoardDbContext db, DateTimeOffset nowUtc) =>
        task => new TaskDetailDto(
            task.Id,
            task.Project.Key + "-" + task.Number,
            task.ProjectId,
            task.Project.Name,
            task.BoardColumnId,
            task.BoardColumn.Name,
            task.BoardColumn.Category,
            task.Title,
            task.Description,
            task.Priority,
            task.DueAtUtc,
            task.DueAtUtc != null && task.CompletedAtUtc == null && task.DueAtUtc < nowUtc,
            task.StartedAtUtc,
            task.CompletedAtUtc,
            task.ParentTaskId,
            task.ParentTask != null ? task.ParentTask.Title : null,
            task.CreatedById,
            db.Users.Where(user => user.Id == task.CreatedById).Select(user => user.DisplayName).First(),
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            task.Assignments
                .Select(assignment => new AssigneeDto(
                    assignment.UserId,
                    assignment.User.DisplayName,
                    assignment.User.Email))
                .ToList(),
            task.Labels
                .Select(applied => new LabelChipDto(applied.Label.Id, applied.Label.Name, applied.Label.ColorHex))
                .ToList(),
            task.Subtasks
                .OrderBy(subtask => subtask.Number)
                .Select(subtask => new SubtaskDto(
                    subtask.Id,
                    task.Project.Key + "-" + subtask.Number,
                    subtask.Title,
                    subtask.CompletedAtUtc != null,
                    subtask.Priority))
                .ToList(),
            task.RowVersion);
}
