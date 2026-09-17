using System.ComponentModel.DataAnnotations;

namespace WorkBoard.Infrastructure.Authentication;

/// <summary>
/// JWT signing and lifetime settings.
/// </summary>
/// <remarks>
/// <para>
/// Validated at startup with <c>ValidateOnStart</c>, so a missing or too-short signing key stops the
/// process from booting instead of surfacing as a 500 on the first login. The key itself never
/// appears in <c>appsettings.json</c> — it comes from user secrets locally and from the environment
/// in every other case.
/// </para>
/// <para>
/// The access-token lifetime is short on purpose. An access token cannot be revoked — that is what
/// makes it cheap to validate, since no request has to hit the database — so the only bound on a
/// leaked one is its expiry. Fifteen minutes is the usual compromise; the refresh token carries the
/// long-lived session and <em>is</em> revocable, because it is stored.
/// </para>
/// </remarks>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum key length. HMAC-SHA256 keys shorter than the 256-bit output weaken the MAC.</summary>
    public const int MinimumKeyLength = 32;

    [Required]
    [MinLength(MinimumKeyLength, ErrorMessage = "The JWT signing key must be at least 32 characters.")]
    public string SigningKey { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = "workboard";

    [Required]
    public string Audience { get; init; } = "workboard-web";

    [Range(1, 120)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 14;
}
