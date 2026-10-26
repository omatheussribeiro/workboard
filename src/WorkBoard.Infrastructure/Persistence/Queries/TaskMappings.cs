using System.Linq.Expressions;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

internal static class TaskMappings
{
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

    public static Expression<Func<WorkItem, TaskDetailDto>> ToDetail(WorkBoardDbContext context, DateTimeOffset nowUtc) =>
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
            context.Users.Where(user => user.Id == task.CreatedById).Select(user => user.DisplayName).First(),
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
