using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>
/// A stored refresh token together with the account it was issued to.
/// </summary>
/// <remarks>
/// Both are returned explicitly rather than reached through <c>token.User</c>. That navigation is
/// populated by EF and null on any token the domain created itself, so a service that relied on it
/// could only be exercised against a database — and a missing <c>Include</c> would surface as a
/// <see cref="NullReferenceException"/> inside the refresh flow rather than as a compile error.
/// </remarks>
/// <param name="Token">The stored token, tracked, ready to be rotated or revoked.</param>
/// <param name="User">The account it belongs to, tracked.</param>
public sealed record RefreshTokenOwner(RefreshToken Token, User User);
