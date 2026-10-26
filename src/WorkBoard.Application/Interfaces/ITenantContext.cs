namespace WorkBoard.Application.Interfaces;

/// <summary>
/// The workspace the current request is acting inside.
/// </summary>
/// <remarks>
/// <para>
/// Resolved once per request from the route (or the <c>X-Workspace-Id</c> header) and validated
/// against the membership table before any use case runs. From then on the value is ambient: the EF
/// Core global query filter reads it, so every tenant-scoped query is filtered whether or not the
/// handler remembered to.
/// </para>
/// <para>
/// This is the single most important object in the security model. Anything that sets it must have
/// already proven membership — see the API's <c>WorkspaceRoleHandler</c> and the isolation tests.
/// </para>
/// </remarks>
public interface ITenantContext
{
    /// <summary>Null on endpoints that are not workspace-scoped, such as login.</summary>
    Guid? WorkspaceId { get; }

    bool HasWorkspace => WorkspaceId is not null;

    /// <summary>The resolved workspace, or a failure if the request was not workspace-scoped.</summary>
    Guid RequireWorkspaceId();
}
