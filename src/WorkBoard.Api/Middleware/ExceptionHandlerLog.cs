namespace WorkBoard.Api.Middleware;

/// <summary>Source-generated log messages for <see cref="ProblemDetailsExceptionHandler"/>.</summary>
internal static partial class ExceptionHandlerLog
{
    [LoggerMessage(EventId = 9000, Level = LogLevel.Error, Message = "Unhandled exception")]
    public static partial void UnhandledException(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 9001,
        Level = LogLevel.Information,
        Message = "Request failed with {ExceptionType} -> {StatusCode}: {Reason}")]
    public static partial void HandledFailure(this ILogger logger, string exceptionType, int statusCode, string reason);
}
