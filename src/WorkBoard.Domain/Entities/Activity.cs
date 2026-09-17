using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// One entry in the change history.
/// </summary>
/// <remarks>
/// <para>
/// Activities are written from domain events inside the same transaction as the change itself, so
/// the feed can never drift from the data it describes. They are append-only: nothing in the
/// application updates or deletes a row.
/// </para>
/// <para>
/// The varying part of each entry is kept as a small JSON payload rather than as a wide table of
/// mostly-null columns. Activities are written constantly and read as a reverse-chronological feed,
/// almost never queried by their payload, so a schema that stays stable as new activity types are
/// added is worth more than queryability of individual fields. When one field does need querying —
/// which task an entry belongs to — it is promoted to a real indexed column.
/// </para>
/// </remarks>
public sealed class Activity : Entity, IWorkspaceScoped
{
    private Activity() { }

    private Activity(Guid id, Guid workspaceId, ActivityType type, Guid actorId, DateTimeOffset occurredAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Type = type;
        ActorId = actorId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid WorkspaceId { get; private set; }

    public ActivityType Type { get; private set; }

    /// <summary>Who caused it. Never null: every mutation in the system is made by an authenticated user.</summary>
    public Guid ActorId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? TaskId { get; private set; }

    /// <summary>Human-readable summary, rendered by the client without needing to parse the payload.</summary>
    public string Summary { get; private set; } = null!;

    /// <summary>Type-specific detail as JSON. Mapped to nvarchar(max), not a SQL Server json column.</summary>
    public string? PayloadJson { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static Activity Record(
        Guid workspaceId,
        ActivityType type,
        Guid actorId,
        string summary,
        DateTimeOffset occurredAtUtc,
        Guid? projectId = null,
        Guid? taskId = null,
        string? payloadJson = null) =>
        new(Guid.CreateVersion7(), Guard.NotDefault(workspaceId), type, Guard.NotDefault(actorId), occurredAtUtc)
        {
            Summary = Guard.MaxLength(Guard.NotEmpty(summary), 500),
            ProjectId = projectId,
            TaskId = taskId,
            PayloadJson = payloadJson,
        };
}
