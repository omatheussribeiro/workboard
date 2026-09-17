using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Services;

/// <summary>
/// Issues the token pair that registration, login and refresh all return.
/// </summary>
/// <remarks>
/// Shared because three use cases produce byte-identical responses, and the interesting part — how
/// long an access token lives, what goes in the body — is a decision that has to be made once. See
/// ADR-006 for why the refresh token travels in the body rather than in a cookie.
/// </remarks>
public interface ITokenIssuer
{
    /// <summary>Issues a fresh refresh token for the user, commits, and builds the response.</summary>
    Task<AuthResponse> IssueAsync(User user, CancellationToken cancellationToken);

    /// <summary>Builds the response around a refresh token the caller has already issued and saved.</summary>
    AuthResponse BuildResponse(User user, string refreshToken);
}
