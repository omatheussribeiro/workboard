using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>Issues the access/refresh token pair and hashes refresh tokens for storage.</summary>
public interface ITokenService
{
    /// <summary>A signed, short-lived JWT carrying the user identity only — never workspace claims.</summary>
    string CreateAccessToken(User user);

    /// <summary>Returns the opaque token handed to the client and the hash persisted alongside it.</summary>
    (string Token, string TokenHash) CreateRefreshToken();

    string HashRefreshToken(string token);

    TimeSpan AccessTokenLifetime { get; }

    TimeSpan RefreshTokenLifetime { get; }
}
