using System.Security.Claims;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.Persistence.Context;
using WorkBoard.Infrastructure.Authentication;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Api.Security;

/// <summary>
/// Projects the HTTP principal onto <see cref="ICurrentUser"/>.
/// </summary>
/// <remarks>
/// The adapter that keeps <c>HttpContext</c> out of the application layer. It reads identity from
/// the validated token and the workspace role from <see cref="WorkspaceContext"/> — never from a
/// claim, because a claim would be as stale as the token carrying it.
/// </remarks>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor, WorkspaceContext workspaceContext)
    : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? Principal?.FindFirst("sub")?.Value;

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsSystemAdmin => Principal?.IsInRole(WorkBoardClaims.SystemAdminRole) == true;

    public WorkspaceRole? WorkspaceRole => workspaceContext.Role;

    public Guid RequireUserId() =>
        UserId ?? throw new InvalidOperationException("This operation requires an authenticated user.");
}
