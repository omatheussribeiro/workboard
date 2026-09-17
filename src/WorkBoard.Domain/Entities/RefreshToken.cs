using WorkBoard.Domain.Exceptions;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// One issued refresh token.
/// </summary>
/// <remarks>
/// Only a SHA-256 hash of the token is stored. A refresh token is a bearer credential with a long
/// lifetime; if the table leaks, hashes cannot be replayed. The rotation chain
/// (<see cref="ReplacedByTokenHash"/>) makes reuse of an already-rotated token detectable, which is
/// the standard mitigation for a stolen token. See ADR-006.
/// </remarks>
public sealed class RefreshToken : Entity
{
    private RefreshToken() { }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevokedReason { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActive => RevokedAtUtc is null;

    internal static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset createdAtUtc) =>
        new(Guid.CreateVersion7(), Guard.NotDefault(userId), Guard.NotEmpty(tokenHash), expiresAtUtc, createdAtUtc);

    public bool IsUsable(DateTimeOffset nowUtc) => IsActive && ExpiresAtUtc > nowUtc;

    public void Revoke(string reason, DateTimeOffset? atUtc = null)
    {
        if (!IsActive)
        {
            return;
        }

        RevokedAtUtc = atUtc ?? DateTimeOffset.UtcNow;
        RevokedReason = reason;
    }

    public void RotateTo(string replacementTokenHash, DateTimeOffset atUtc)
    {
        Revoke("rotated", atUtc);
        ReplacedByTokenHash = Guard.NotEmpty(replacementTokenHash);
    }
}
