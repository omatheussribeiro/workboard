using Microsoft.AspNetCore.Authorization;
using WorkBoard.Application.Interfaces;
using WorkBoard.Api.Security;
using WorkBoard.Infrastructure.Authentication;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Api.Extensions;

/// <summary>Registers the authorization policies and the handler behind them.</summary>
public static class AuthorizationRegistration
{
    /// <summary>Adds the workspace and system-admin policies, and the fail-closed fallback.</summary>
    public static IServiceCollection AddWorkBoardAuthorization(this IServiceCollection services)
    {
        services.AddScoped<WorkspaceContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<WorkspaceContext>());
        services.AddScoped<IAuthorizationHandler, WorkspaceRoleHandler>();

        services.AddAuthorization(options =>
        {
            // Both workspace policies use the same handler with a different minimum role, so
            // membership is resolved — and the tenant bound — in exactly one place.
            options.AddPolicy(WorkBoardClaims.WorkspaceMemberPolicy, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new WorkspaceRoleRequirement(WorkspaceRole.Member)));

            options.AddPolicy(WorkBoardClaims.WorkspaceAdminPolicy, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new WorkspaceRoleRequirement(WorkspaceRole.Admin)));

            options.AddPolicy(WorkBoardClaims.SystemAdminPolicy, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(WorkBoardClaims.SystemAdminRole));

            // Nothing is anonymous unless it says so. An endpoint added without a policy fails
            // closed instead of shipping unauthenticated.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
