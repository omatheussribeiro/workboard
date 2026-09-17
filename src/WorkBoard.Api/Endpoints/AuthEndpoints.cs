using System.Security.Claims;
using WorkBoard.Api.Filters;
using WorkBoard.Api.Extensions;
using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Application.UseCases.Auth.Login;
using WorkBoard.Application.UseCases.Auth.Logout;
using WorkBoard.Application.UseCases.Auth.RefreshToken;
using WorkBoard.Application.UseCases.Auth.Register;

namespace WorkBoard.Api.Endpoints;

/// <summary>
/// Registration, login, refresh and logout.
/// </summary>
/// <remarks>
/// The only anonymous group in the API, and the only one under the stricter <c>auth</c> rate-limit
/// policy: these endpoints are reachable without a token, so they are what an attacker hammers.
/// </remarks>
public static class AuthEndpoints
{
    /// <summary>Maps the authentication routes.</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth);

        group.MapPost("/register", async (
                RegisterRequest request,
                IRegisterUseCase register,
                CancellationToken cancellationToken) =>
            Results.Ok(await register.ExecuteAsync(request, cancellationToken)))
            .WithValidation<RegisterRequest>()
            .WithName("Register")
            .WithSummary("Creates an account and returns a token pair.")
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (
                LoginRequest request,
                ILoginUseCase login,
                CancellationToken cancellationToken) =>
            Results.Ok(await login.ExecuteAsync(request, cancellationToken)))
            .WithValidation<LoginRequest>()
            .WithName("Login")
            .WithSummary("Exchanges credentials for a token pair.")
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/refresh", async (
                RefreshRequest request,
                IRefreshTokenUseCase refresh,
                CancellationToken cancellationToken) =>
            Results.Ok(await refresh.ExecuteAsync(request, cancellationToken)))
            .WithValidation<RefreshRequest>()
            .WithName("Refresh")
            .WithSummary("Rotates a refresh token and issues a new pair.")
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/logout", async (
                RefreshRequest request,
                ILogoutUseCase logout,
                CancellationToken cancellationToken) =>
            {
                await logout.ExecuteAsync(request.RefreshToken, cancellationToken);
                return Results.NoContent();
            })
            .WithValidation<RefreshRequest>()
            .WithName("Logout")
            .WithSummary("Revokes a refresh token.")
            .Produces(StatusCodes.Status204NoContent);

        // Outside the group: it is the one route here that requires a valid access token, and it
        // exists so a client holding a token can ask who it belongs to without decoding the JWT.
        app.MapGet("/api/auth/me", (HttpContext context) => Results.Ok(new
            {
                id = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                email = context.User.FindFirst(ClaimTypes.Email)?.Value,
                displayName = context.User.Identity?.Name,
            }))
            .WithTags("Auth")
            .RequireAuthorization()
            .WithName("CurrentUser")
            .WithSummary("Returns the identity behind the current access token.");

        return app;
    }
}
