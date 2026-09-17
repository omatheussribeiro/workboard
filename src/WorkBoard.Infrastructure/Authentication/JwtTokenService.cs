using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Infrastructure.Authentication;

/// <summary>
/// Issues access tokens and refresh tokens.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(_options.AccessTokenMinutes);

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    /// <summary>
    /// Builds the access token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The token carries identity and nothing else: user id, email, display name, and the system-admin
    /// flag. It deliberately does <em>not</em> carry workspace membership.
    /// </para>
    /// <para>
    /// Putting roles per workspace in the token would avoid a database read per request, and would
    /// mean that removing someone from a workspace leaves them with access until their token expires
    /// — up to fifteen minutes of unauthorised access to data they were just removed from. Membership
    /// is therefore resolved per request from the <c>WorkspaceMembers</c> table, which is a
    /// single-row seek on a composite primary key. See ADR-006.
    /// </para>
    /// </remarks>
    public string CreateAccessToken(User user)
    {
        var now = clock.UtcNow;

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email,
            [JwtRegisteredClaimNames.Name] = user.DisplayName,
            // A unique token id, so an individual token can be identified in logs without the token
            // itself ever being logged.
            [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
        };

        if (user.IsSystemAdmin)
        {
            claims[ClaimTypes.Role] = WorkBoardClaims.SystemAdminRole;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Claims = claims,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.Add(AccessTokenLifetime).UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        };

        return _handler.CreateToken(descriptor);
    }

    /// <summary>
    /// Mints a refresh token and the hash that will be stored for it.
    /// </summary>
    /// <remarks>
    /// The token is 256 bits of cryptographic randomness with no structure — it is a lookup key, not
    /// a claims carrier, so there is nothing for a client to read and nothing for an attacker to
    /// forge. Only the SHA-256 hash is persisted: a plain SHA-256 is right here where it would be
    /// wrong for a password, because the input is already high-entropy and cannot be brute-forced,
    /// and this value is checked on every refresh where a slow KDF would be a real cost.
    /// </remarks>
    public (string Token, string TokenHash) CreateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return (token, HashRefreshToken(token));
    }

    public string HashRefreshToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    internal SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(_options.SigningKey));
}
