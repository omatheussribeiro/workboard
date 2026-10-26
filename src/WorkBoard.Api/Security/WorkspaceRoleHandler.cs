using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Api.Security;

/// <summary>
/// Resolves the workspace from the route, proves membership, and binds the request to that tenant.
/// </summary>
/// <remarks>
/// <para>
/// This handler is the hinge of the security model, and it is worth being clear about why it is an
/// authorisation handler and not middleware.
/// </para>
/// <para>
/// Middleware runs for every request, so it would have to guess which routes are workspace-scoped,
/// and it runs before the endpoint has been authorised. An authorisation handler runs only for
/// endpoints that asked for the policy, and it runs at the point where a failure is already a clean
/// 403. Making the tenant binding a side effect of passing the policy means the two cannot diverge:
/// there is no path that reaches a service with a workspace set but unverified, because the same
/// code does both.
/// </para>
/// <para>
/// Membership is read from the database on every request rather than from a token claim. That costs
/// one seek on the <c>WorkspaceMembers</c> composite primary key, and it buys immediate revocation —
/// removing someone takes effect now, not when their access token expires.
/// </para>
/// </remarks>
public sealed class WorkspaceRoleHandler(
    IHttpContextAccessor httpContextAccessor,
    IWorkspaceQueries workspaceQueries,
    WorkspaceContext workspaceContext,
    ILogger<WorkspaceRoleHandler> logger)
    : AuthorizationHandler<WorkspaceRoleRequirement>
{
    /// <summary>Route parameter carrying the workspace id.</summary>
    public const string RouteKey = "workspaceId";

    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkspaceRoleRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        if (!TryGetUserId(context, out var userId) || !TryGetWorkspaceId(httpContext, out var workspaceId))
        {
            // No Fail() call: leaving the requirement unmet is enough, and Fail() would
            // short-circuit any other policy evaluated on the same request.
            return;
        }

        // This query must run before the tenant is known, so its Infrastructure adapter bypasses
        // the tenant filter while anchoring the lookup on the caller's own user id.
        var role = await workspaceQueries.GetRoleForUserAsync(
            workspaceId,
            userId,
            httpContext.RequestAborted);

        if (role is null || role < requirement.MinimumRole)
        {
            logger.WorkspaceAccessDenied(userId, workspaceId);
            return;
        }

        // Only reached once membership and role are proven. Everything downstream — every DbSet, via
        // the global query filter — is now scoped to this workspace.
        workspaceContext.Enter(workspaceId, role.Value);
        context.Succeed(requirement);
    }

    private static bool TryGetUserId(AuthorizationHandlerContext context, out Guid userId)
    {
        userId = Guid.Empty;

        var value = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("sub")?.Value;

        return value is not null && Guid.TryParse(value, out userId);
    }

    private static bool TryGetWorkspaceId(HttpContext httpContext, out Guid workspaceId)
    {
        workspaceId = Guid.Empty;

        return httpContext.Request.RouteValues.TryGetValue(RouteKey, out var raw)
               && Guid.TryParse(raw?.ToString(), out workspaceId);
    }
}
