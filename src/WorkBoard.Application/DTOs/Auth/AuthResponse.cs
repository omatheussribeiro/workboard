namespace WorkBoard.Application.DTOs.Auth;

/// <summary>
/// What the client receives after a successful registration, login or refresh.
/// </summary>
/// <remarks>
/// The refresh token is returned in the body rather than set as a cookie because the SPA and the
/// API are deployed as separate origins; a cookie would need third-party-cookie tolerance that no
/// longer exists in current browsers. The consequence is that the client is responsible for storing
/// it, and the mitigation is that refresh tokens rotate on every use and are stored only as hashes.
/// See ADR-006.
/// </remarks>
/// <param name="AccessToken">Short-lived signed JWT.</param>
/// <param name="RefreshToken">Opaque rotating token.</param>
/// <param name="AccessTokenExpiresAtUtc">When the access token stops being accepted.</param>
/// <param name="User">The authenticated user.</param>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    AuthenticatedUserDto User);
