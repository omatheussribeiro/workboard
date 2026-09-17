using Microsoft.Extensions.Logging;
using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Logging;
using WorkBoard.Application.Services;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.UseCases.Auth.Register;

/// <inheritdoc cref="IRegisterUseCase"/>
/// <remarks>
/// Reads past the tenant filter through <see cref="IUserRepository"/>, and has to: at this point in
/// a request no workspace has been resolved, because the caller has not yet been authenticated.
/// </remarks>
public sealed class RegisterUseCase(
    IUserRepository users,
    ITokenIssuer tokenIssuer,
    IPasswordHasher passwordHasher,
    ILogger<RegisterUseCase> logger) : IRegisterUseCase
{
    /// <inheritdoc/>
    public async Task<AuthResponse> ExecuteAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await users.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            // Deliberately explicit. Registration is already an email-enumeration oracle — the
            // alternative (silently pretending to succeed) breaks the client for no real gain, since
            // an attacker learns the same thing from a password-reset flow. Rate limiting is the
            // control that actually applies here; see the "auth" rate-limit policy.
            throw new ConflictException("An account with that email already exists.");
        }

        var user = User.Register(normalizedEmail, request.DisplayName, passwordHasher.Hash(request.Password));
        users.Add(user);

        var response = await tokenIssuer.IssueAsync(user, cancellationToken);
        logger.UserRegistered(user.Id);
        return response;
    }
}
