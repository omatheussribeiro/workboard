namespace WorkBoard.Infrastructure.Authentication;

/// <summary>
/// Claim values and authorization policy names, in one place so the API, the token issuer and the
/// tests cannot drift apart.
/// </summary>
/// <remarks>
/// String constants rather than an enum, because that is what both ends actually consume: the token
/// handler writes a claim value and <c>RequireAuthorization</c> takes a policy name. Keeping them
/// here means a typo is a compile error at one of the two ends rather than an endpoint that silently
/// requires a policy nobody registered — which fails closed, but with a 403 that takes a while to
/// explain.
/// </remarks>
public static class WorkBoardClaims
{
    /// <summary>Role value written into the access token for platform administrators.</summary>
    public const string SystemAdminRole = "SystemAdmin";

    /// <summary>Requires membership of the workspace in the route.</summary>
    public const string WorkspaceMemberPolicy = "WorkspaceMember";

    /// <summary>Requires at least the Admin role in the workspace in the route.</summary>
    public const string WorkspaceAdminPolicy = "WorkspaceAdmin";

    /// <summary>Requires the platform-wide administrator role.</summary>
    public const string SystemAdminPolicy = "SystemAdmin";
}
