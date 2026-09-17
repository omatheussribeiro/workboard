using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>
/// Write-side access to users and their refresh tokens.
/// </summary>
/// <remarks>
/// Authentication happens before any workspace is resolved, so every method here reads past the
/// tenant filter by necessity. That is safe precisely because none of them accepts a workspace: they
/// are keyed by normalised email or by token hash, both of which the caller already possesses.
/// </remarks>
public interface IUserRepository
{
    /// <summary>Finds a user by normalised email, or <c>null</c>.</summary>
    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Whether the normalised email is already registered.</summary>
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Finds a refresh token by its stored hash, together with the account that owns it.</summary>
    Task<RefreshTokenOwner?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Every refresh token of a user that has not been revoked yet.</summary>
    Task<IReadOnlyList<RefreshToken>> ListActiveRefreshTokensAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Stages a new user.</summary>
    void Add(User user);

    /// <summary>Stages a new refresh token.</summary>
    void AddRefreshToken(RefreshToken token);
}
