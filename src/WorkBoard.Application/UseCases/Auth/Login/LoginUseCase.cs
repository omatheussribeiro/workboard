using Microsoft.Extensions.Logging;
using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Logging;
using WorkBoard.Application.Services;

namespace WorkBoard.Application.UseCases.Auth.Login;

/// <inheritdoc cref="ILoginUseCase"/>
public sealed class LoginUseCase(
    IUserRepository users,
    ITokenIssuer tokenIssuer,
    IPasswordHasher passwordHasher,
    ILogger<LoginUseCase> logger) : ILoginUseCase
{
    /// <summary>A real BCrypt hash of a value nobody knows, used purely to equalise login timing.</summary>
    private const string DummyHash = "$2a$12$9dQ8kPq3dFf3M4Yl0h3lXeRQq8Q8mUwZ1oQdF9bGq1kZq0Jm4Nq7O";

    /// <inheritdoc/>
    public async Task<AuthResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailAsync(normalizedEmail, cancellationToken);

        // Verify against a dummy hash when the user does not exist, so a missing account and a wrong
        // password take the same time. Without this, response latency alone reveals which addresses
        // are registered — BCrypt at cost 12 is slow enough for the difference to be measurable over
        // the network.
        var passwordMatches = user is null
            ? passwordHasher.Verify(request.Password, DummyHash) && false
            : passwordHasher.Verify(request.Password, user.PasswordHash);

        if (user is null || !passwordMatches || !user.IsActive)
        {
            logger.LoginFailed(normalizedEmail);
            throw new ForbiddenException("Invalid credentials.");
        }

        return await tokenIssuer.IssueAsync(user, cancellationToken);
    }
}
