using Microsoft.Extensions.Logging;
using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Logging;
using WorkBoard.Application.Services;

namespace WorkBoard.Application.UseCases.Auth.RefreshToken;

/// <inheritdoc cref="IRefreshTokenUseCase"/>
/// <remarks>
/// Presenting a token that was already rotated means either a replay or a stolen token, and the two
/// are indistinguishable from here. The response is to revoke the entire chain for that user: an
/// attacker who replays a captured token logs the legitimate user out, which is noticeable, rather
/// than quietly gaining a parallel session, which is not.
/// </remarks>
public sealed class RefreshTokenUseCase(
    IUserRepository users,
    ITokenIssuer tokenIssuer,
    ITokenService tokens,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RefreshTokenUseCase> logger) : IRefreshTokenUseCase
{
    private const string InvalidRefreshToken = "Invalid refresh token.";

    /// <inheritdoc/>
    public async Task<AuthResponse> ExecuteAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var hash = tokens.HashRefreshToken(request.RefreshToken);

        // Every failure below returns the same message. Distinguishing "unknown token" from "expired"
        // from "revoked" would tell whoever is probing which state they have reached.
        var (stored, user) = await users.FindRefreshTokenAsync(hash, cancellationToken)
                             ?? throw new ForbiddenException(InvalidRefreshToken);

        if (!stored.IsUsable(nowUtc))
        {
            if (stored.ReplacedByTokenHash is not null)
            {
                logger.RefreshTokenReuseDetected(stored.UserId);
                await RevokeEveryTokenAsync(stored.UserId, cancellationToken);
            }

            throw new ForbiddenException(InvalidRefreshToken);
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException(InvalidRefreshToken);
        }

        var (replacement, replacementHash) = tokens.CreateRefreshToken();
        stored.RotateTo(replacementHash, nowUtc);
        user.IssueRefreshToken(replacementHash, nowUtc.Add(tokens.RefreshTokenLifetime), nowUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tokenIssuer.BuildResponse(user, replacement);
    }

    private async Task RevokeEveryTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        foreach (var token in await users.ListActiveRefreshTokensAsync(userId, cancellationToken))
        {
            token.Revoke("token-reuse-detected", clock.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
