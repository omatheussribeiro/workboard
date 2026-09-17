using Microsoft.Extensions.Logging;

namespace WorkBoard.Application.Logging;

/// <summary>
/// Every log message the application layer emits, as source-generated delegates.
/// </summary>
/// <remarks>
/// <para>
/// Written with <c>[LoggerMessage]</c> rather than <c>logger.LogInformation($"...")</c> for three
/// reasons the analyser (CA1848/CA1873) is right about: the generator emits a cached delegate
/// instead of allocating an object array and boxing every argument on each call; the message
/// template is checked against the parameters at compile time; and the arguments are not evaluated
/// at all when the level is disabled.
/// </para>
/// <para>
/// Collecting them in one place also makes the event-id space reviewable, which matters once alerts
/// are built on top of it. Ids are grouped by area and never reused.
/// </para>
/// </remarks>
public static partial class Log
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Registered user {UserId}")]
    public static partial void UserRegistered(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Failed login attempt for {Email}")]
    public static partial void LoginFailed(this ILogger logger, string email);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId}; revoking every active session")]
    public static partial void RefreshTokenReuseDetected(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "User {UserId} logged out")]
    public static partial void UserLoggedOut(this ILogger logger, Guid userId);

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Warning,
        Message = "User {UserId} was denied access to workspace {WorkspaceId}")]
    public static partial void WorkspaceAccessDenied(this ILogger logger, Guid userId, Guid workspaceId);

    [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "Rebalanced positions in column {ColumnId}")]
    public static partial void ColumnRebalanced(this ILogger logger, Guid columnId);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Concurrency conflict on task {TaskId}; the caller was asked to reload")]
    public static partial void TaskConcurrencyConflict(this ILogger logger, Guid taskId);
}
