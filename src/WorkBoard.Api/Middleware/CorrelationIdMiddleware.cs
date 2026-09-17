using System.Diagnostics;
using Serilog.Context;

namespace WorkBoard.Api.Middleware;

/// <summary>
/// Gives every request a correlation id and puts it on the logs, the response and the trace.
/// </summary>
/// <remarks>
/// <para>
/// An incoming <c>X-Correlation-Id</c> is honoured so a request that crosses services keeps one id
/// end to end; otherwise the id of the current <see cref="Activity"/> is used, which is the W3C
/// trace id that OpenTelemetry already propagates. Falling back to the trace id rather than minting
/// a fresh GUID means logs and traces agree on one identifier instead of two.
/// </para>
/// <para>
/// An inbound header is length-capped and filtered before being used. It ends up in log output and
/// in a response header, so an unbounded caller-supplied string is both a log-injection vector and a
/// way to bloat every log line.
/// </para>
/// </remarks>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>The request and response header carrying the id.</summary>
    public const string HeaderName = "X-Correlation-Id";

    private const int MaxLength = 64;

    /// <summary>Resolves the id, publishes it, and runs the rest of the pipeline inside its log scope.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Resolve(context);

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        // Pushed onto the Serilog context so every log written during this request carries it,
        // including those from EF Core and the framework, which know nothing about correlation ids.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string Resolve(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(incoming))
        {
            var trimmed = incoming.Length > MaxLength ? incoming[..MaxLength] : incoming;

            // Only characters that are safe in a header and in a log line. Anything else and the id
            // is regenerated rather than sanitised further.
            if (trimmed.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
            {
                return trimmed;
            }
        }

        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }
}
