using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Auth;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Services;

/// <inheritdoc cref="ITokenIssuer"/>
public sealed class TokenIssuer(ITokenService tokens, IUnitOfWork unitOfWork, IClock clock) : ITokenIssuer
{
    /// <inheritdoc/>
    public async Task<AuthResponse> IssueAsync(User user, CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var (token, tokenHash) = tokens.CreateRefreshToken();

        // Only the hash is stored. A leaked database therefore yields no usable session, which is the
        // whole reason the opaque token is not persisted as issued.
        user.IssueRefreshToken(tokenHash, nowUtc.Add(tokens.RefreshTokenLifetime), nowUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BuildResponse(user, token);
    }

    /// <inheritdoc/>
    public AuthResponse BuildResponse(User user, string refreshToken) =>
        new(tokens.CreateAccessToken(user),
            refreshToken,
            clock.UtcNow.Add(tokens.AccessTokenLifetime),
            user.ToDto());
}
