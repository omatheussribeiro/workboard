namespace WorkBoard.Api.Middleware;

/// <summary>
/// Adds the response headers that constrain what a browser will do with this response.
/// </summary>
/// <remarks>
/// <para>
/// Application endpoints serve JSON, which shapes the whole set. <c>X-Content-Type-Options</c>
/// stops a browser sniffing a JSON response as something executable. The CSP is the most restrictive
/// one there is — <c>default-src 'none'</c> — because a JSON endpoint has no legitimate reason to
/// load anything at all; it matters for the case where a response is somehow rendered directly.
/// Scalar and Swagger are the deliberate exception: both are HTML applications that execute their
/// own scripts, so the JSON-only policy is omitted for their paths. The remaining headers still
/// apply to those pages.
/// </para>
/// <para>
/// HSTS is not set here. It belongs on the edge that terminates TLS, and setting it from a container
/// that may be reached over plain HTTP behind a proxy is how a development environment becomes
/// permanently unreachable in a browser.
/// </para>
/// </remarks>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>Sets the headers, then runs the rest of the pipeline.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        if (!IsApiReference(context.Request.Path))
        {
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        // Removed rather than set: advertising the server product helps nobody but an attacker
        // matching versions against known vulnerabilities.
        headers.Remove("Server");

        await next(context);
    }

    private static bool IsApiReference(PathString path) =>
        path.StartsWithSegments("/scalar") || path.StartsWithSegments("/swagger");
}
