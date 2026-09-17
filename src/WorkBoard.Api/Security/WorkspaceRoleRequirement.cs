using Microsoft.AspNetCore.Authorization;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Api.Security;

/// <summary>"The caller must be a member of the workspace in the route, with at least this role."</summary>
/// <param name="minimumRole">The least privileged role that satisfies the requirement.</param>
public sealed class WorkspaceRoleRequirement(WorkspaceRole minimumRole) : IAuthorizationRequirement
{
    /// <summary>The least privileged role that satisfies this requirement.</summary>
    public WorkspaceRole MinimumRole { get; } = minimumRole;
}
