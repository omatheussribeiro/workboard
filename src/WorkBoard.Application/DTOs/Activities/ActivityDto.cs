using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.DTOs.Activities;

/// <summary>One entry of the change history.</summary>
/// <param name="Id">Activity identifier.</param>
/// <param name="Type">What kind of change it was.</param>
/// <param name="Summary">Rendered sentence, written when the change happened.</param>
/// <param name="ActorId">Who made the change.</param>
/// <param name="ActorName">Their display name.</param>
/// <param name="ProjectId">Project the change belongs to, if any.</param>
/// <param name="TaskId">Card the change belongs to, if any.</param>
/// <param name="PayloadJson">Before/after detail, as written by the dispatcher.</param>
/// <param name="OccurredAtUtc">When it happened.</param>
public sealed record ActivityDto(
    Guid Id,
    ActivityType Type,
    string Summary,
    Guid ActorId,
    string ActorName,
    Guid? ProjectId,
    Guid? TaskId,
    string? PayloadJson,
    DateTimeOffset OccurredAtUtc);
