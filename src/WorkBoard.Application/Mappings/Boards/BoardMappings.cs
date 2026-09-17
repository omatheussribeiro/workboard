using System.Linq.Expressions;
using WorkBoard.Application.DTOs.Boards;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Mappings.Boards;

/// <summary>
/// The board projection — the hottest read in the product, and the one most likely to be written
/// badly, so it is worth being explicit about what it does and does not do.
/// </summary>
/// <remarks>
/// <para>
/// It does <em>not</em> <c>Include</c> columns, then tasks, then assignments, then labels. That
/// version reads correctly and produces a cartesian explosion: a board of 200 tasks with three
/// assignees and three labels each returns roughly 1,800 rows, most of them duplicated task columns
/// including the 10 KB description, for a payload that needs none of it.
/// </para>
/// <para>
/// Instead the entire shape is one projection. EF Core translates the nested <c>Select</c>s into a
/// single statement with correlated subqueries, reads only the listed columns, and — because
/// nothing is materialised as an entity — does no change tracking at all. The counts become
/// <c>COUNT</c> subqueries rather than collections loaded in order to be counted in memory. The
/// covering index <c>IX_Tasks_BoardColumnId_Position</c> backs the per-column ordering.
/// </para>
/// </remarks>
public static class BoardMappings
{
    /// <summary>Projects a board onto its full client representation.</summary>
    /// <param name="nowUtc">Evaluation instant for <c>IsOverdue</c>, so the whole board agrees with itself.</param>
    public static Expression<Func<Board, BoardDto>> ToBoard(DateTimeOffset nowUtc) =>
        board => new BoardDto(
            board.Id,
            board.Name,
            board.ProjectId,
            board.Project.Key,
            board.Columns
                .OrderBy(column => column.Position)
                .Select(column => new BoardColumnDto(
                    column.Id,
                    column.Name,
                    column.Position,
                    column.Category,
                    column.WipLimit,
                    column.Tasks.Count(task => task.ParentTaskId == null),
                    column.Tasks
                        .Where(task => task.ParentTaskId == null)
                        .OrderBy(task => task.Position)
                        .Select(task => new BoardCardDto(
                            task.Id,
                            board.Project.Key + "-" + task.Number,
                            task.Title,
                            task.Priority,
                            task.Position,
                            task.DueAtUtc,
                            task.DueAtUtc != null && task.CompletedAtUtc == null && task.DueAtUtc < nowUtc,
                            task.Comments.Count(comment => !comment.IsDeleted),
                            task.Subtasks.Count,
                            task.Subtasks.Count(subtask => subtask.CompletedAtUtc != null),
                            task.Assignments
                                .Select(assignment => new BoardAssigneeDto(
                                    assignment.UserId,
                                    assignment.User.DisplayName))
                                .ToList(),
                            task.Labels
                                .Select(applied => new LabelChipDto(
                                    applied.Label.Id,
                                    applied.Label.Name,
                                    applied.Label.ColorHex))
                                .ToList(),

                            // Projected as raw bytes: System.Text.Json renders byte[] as base64,
                            // whereas Convert.ToBase64String has no SQL translation and would force
                            // the whole query to evaluate on the client.
                            task.RowVersion))
                        .ToList()))
                .ToList());
}
