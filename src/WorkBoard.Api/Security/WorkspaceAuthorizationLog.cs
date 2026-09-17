namespace WorkBoard.Api.Security;

/// <summary>Source-generated log messages for <see cref="WorkspaceRoleHandler"/>.</summary>
internal static partial class WorkspaceAuthorizationLog
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "User {UserId} was denied access to workspace {WorkspaceId}")]
    public static partial void WorkspaceAccessDenied(this ILogger logger, Guid userId, Guid workspaceId);
}
