using WorkBoard.Domain.Events;
using WorkBoard.Domain.Exceptions;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Entities;

/// <summary>
/// A person who can sign in. Users are global, not workspace-scoped: the same account joins many
/// workspaces through <see cref="WorkspaceMember"/>.
/// </summary>
public sealed class User : Entity, IAuditable
{
    private readonly List<WorkspaceMember> _memberships = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User() { }

    private User(Guid id, string email, string displayName, string passwordHash)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
    }

    public string Email { get; private set; } = null!;

    /// <summary>
    /// Lower-cased <see cref="Email"/>. Persisted as a computed-free plain column with a unique
    /// index; comparing on an explicit normalised column keeps the index seekable instead of
    /// relying on collation behaviour that differs between SQL Server and other providers.
    /// </summary>
    public string NormalizedEmail { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    /// <summary>Platform operator. Grants the <c>SystemAdmin</c> policy, never workspace data access.</summary>
    public bool IsSystemAdmin { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<WorkspaceMember> Memberships => _memberships;

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens;

    public static User Register(string email, string displayName, string passwordHash)
    {
        var normalizedEmail = Guard.MaxLength(Guard.NotEmpty(email), 256).ToLowerInvariant();
        Guard.Against(!IsPlausibleEmail(normalizedEmail), "Email is not a valid address.");

        var user = new User(
            Guid.CreateVersion7(),
            normalizedEmail,
            Guard.MaxLength(Guard.NotEmpty(displayName), 120),
            Guard.NotEmpty(passwordHash))
        {
            NormalizedEmail = normalizedEmail,
        };

        user.Raise(new UserRegistered(user.Id, user.Email));
        return user;
    }

    public void ChangeDisplayName(string displayName) =>
        DisplayName = Guard.MaxLength(Guard.NotEmpty(displayName), 120);

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = Guard.NotEmpty(passwordHash);

        // Changing the password invalidates every outstanding session: a stolen refresh token must
        // not survive the remediation that was meant to kill it.
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke("password-changed");
        }
    }

    public void Deactivate() => IsActive = false;

    public RefreshToken IssueRefreshToken(string tokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset nowUtc)
    {
        Guard.Against(!IsActive, "A deactivated user cannot be issued tokens.");
        Guard.Against(expiresAtUtc <= nowUtc, "Refresh token expiry must be in the future.");

        var token = RefreshToken.Issue(Id, tokenHash, expiresAtUtc, nowUtc);
        _refreshTokens.Add(token);
        return token;
    }

    public void PromoteToSystemAdmin() => IsSystemAdmin = true;

    void IAuditable.SetCreated(DateTimeOffset at) => CreatedAtUtc = at;

    void IAuditable.SetUpdated(DateTimeOffset at) => UpdatedAtUtc = at;

    // Deliberately permissive. Real address validation is delivery, not a regex; this only rejects
    // input that is obviously not an address so the unique index stays meaningful.
    private static bool IsPlausibleEmail(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at < value.Length - 1 && value.IndexOf('.', at) > at + 1 && !value.Contains(' ', StringComparison.Ordinal);
    }
}
