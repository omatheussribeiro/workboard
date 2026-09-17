using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Events;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Infrastructure.Services;

/// <summary>
/// Turns domain events into <see cref="Activity"/> rows — the change history.
/// </summary>
/// <remarks>
/// A single <c>switch</c> rather than one handler class per event type resolved by reflection. There
/// are a dozen events, they all produce the same kind of row, and a reader wanting to know "what
/// gets recorded" can answer it by reading one file. The cost is that adding an event means editing
/// this method; that is a deliberate trade, and the compiler points at the missing case because the
/// switch is exhaustive over the types it knows.
/// </remarks>
public sealed class ActivityWriter(IClock clock) : IDomainEventListener
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web);

    public Task HandleAsync(IDomainEvent domainEvent, DbContext context, CancellationToken cancellationToken)
    {
        var activity = Map(domainEvent);
        if (activity is not null)
        {
            context.Add(activity);
        }

        return Task.CompletedTask;
    }

    private Activity? Map(IDomainEvent domainEvent)
    {
        var at = clock.UtcNow;

        return domainEvent switch
        {
            WorkspaceCreated e => Activity.Record(
                e.WorkspaceId, ActivityType.WorkspaceCreated, e.OwnerId, $"created the workspace {e.Name}", at),

            WorkspaceMemberAdded e => Activity.Record(
                e.WorkspaceId, ActivityType.MemberAdded, e.UserId, $"joined the workspace as {e.Role}", at),

            WorkspaceMemberRoleChanged e => Activity.Record(
                e.WorkspaceId, ActivityType.MemberRoleChanged, e.UserId, $"role changed to {e.Role}", at),

            WorkspaceMemberRemoved e => Activity.Record(
                e.WorkspaceId, ActivityType.MemberRemoved, e.UserId, "left the workspace", at),

            ProjectCreated e => Activity.Record(
                e.WorkspaceId, ActivityType.ProjectCreated, e.ActorId, $"created the project {e.Name}", at,
                projectId: e.ProjectId),

            TaskCreated e => Activity.Record(
                e.WorkspaceId, ActivityType.TaskCreated, e.ActorId, $"created {e.Title}", at,
                projectId: e.ProjectId, taskId: e.TaskId),

            TaskMoved e => Activity.Record(
                e.WorkspaceId, ActivityType.TaskMoved, e.ActorId, $"moved the task to {e.ToCategory}", at,
                projectId: e.ProjectId,
                taskId: e.TaskId,
                payloadJson: Serialize(new { from = e.FromColumnId, to = e.ToColumnId })),

            // The only event that carries before/after values, which is exactly what the history
            // panel renders: "changed Priority from Medium to High".
            TaskFieldChanged e => Activity.Record(
                e.WorkspaceId, ActivityType.TaskUpdated, e.ActorId, $"changed {e.Field}", at,
                taskId: e.TaskId,
                payloadJson: Serialize(new { field = e.Field, oldValue = e.OldValue, newValue = e.NewValue })),

            TaskAssigned e => Activity.Record(
                e.WorkspaceId, ActivityType.TaskAssigned, e.ActorId, "assigned the task", at,
                projectId: e.ProjectId,
                taskId: e.TaskId,
                payloadJson: Serialize(new { assigneeId = e.AssigneeId })),

            TaskUnassigned e => Activity.Record(
                e.WorkspaceId, ActivityType.TaskUnassigned, e.AssigneeId, "was unassigned", at,
                taskId: e.TaskId),

            TaskCommented e => Activity.Record(
                e.WorkspaceId, ActivityType.TaskCommented, e.AuthorId, "commented on the task", at,
                projectId: e.ProjectId,
                taskId: e.TaskId,
                payloadJson: Serialize(new { commentId = e.CommentId })),

            // ProjectArchived, UserRegistered and anything added later that is not worth a history
            // entry fall through without one, rather than forcing a row nobody reads.
            _ => null,
        };
    }

    private static string Serialize<T>(T payload) => JsonSerializer.Serialize(payload, PayloadOptions);
}
