namespace WorkBoard.Api.Middleware;

/// <summary>Status codes that are in common use but not in <see cref="StatusCodes"/>.</summary>
internal static class ClientStatusCodes
{
    /// <summary>
    /// 499, from nginx. Not defined in <see cref="StatusCodes"/> because it is not in an RFC, but it
    /// is the right code for a request the client abandoned: the alternative, 500, makes an ordinary
    /// navigation away from a page look like a server fault in every dashboard.
    /// </summary>
    public const int ClientClosedRequest = 499;
}
