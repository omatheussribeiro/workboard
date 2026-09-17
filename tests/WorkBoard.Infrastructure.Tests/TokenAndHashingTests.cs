using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.Authentication;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Infrastructure.Tests;

/// <summary>A clock the test controls, so token lifetimes can be asserted without waiting.</summary>
internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}

public sealed class PasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void A_password_verifies_against_its_own_hash()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        _hasher.Verify("correct horse battery staple", hash).Should().BeTrue();
        _hasher.Verify("Correct horse battery staple", hash).Should().BeFalse();
    }

    [Fact]
    public void The_same_password_hashes_differently_every_time()
    {
        var first = _hasher.Hash("same password");
        var second = _hasher.Hash("same password");

        // BCrypt salts each hash. Equal outputs would mean an unsalted digest, and a rainbow table
        // would then break every account at once.
        first.Should().NotBe(second);
        _hasher.Verify("same password", first).Should().BeTrue();
        _hasher.Verify("same password", second).Should().BeTrue();
    }

    [Fact]
    public void A_corrupt_stored_hash_fails_verification_instead_of_throwing()
    {
        // A 500 here would single out the one account whose row is broken, which is information an
        // attacker can use. It has to look like any other failed login.
        _hasher.Verify("anything", "not-a-bcrypt-hash").Should().BeFalse();
    }
}

public sealed class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        SigningKey = "a-test-signing-key-that-is-long-enough-32",
        Issuer = "workboard-test",
        Audience = "workboard-web-test",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 14,
    };

    private static JwtTokenService NewService(FixedClock clock) => new(Microsoft.Extensions.Options.Options.Create(Options), clock);

    private static User NewUser() => User.Register("ada@workboard.dev", "Ada Lovelace", "hash");

    [Fact]
    public async Task An_access_token_carries_identity_and_is_signed_with_the_configured_key()
    {
        var service = NewService(new FixedClock(Now));
        var user = NewUser();

        var token = service.CreateAccessToken(user);

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Options.SigningKey)),
            ValidateLifetime = false,
        });

        result.IsValid.Should().BeTrue();
        result.Claims[JwtRegisteredClaimNames.Sub].Should().Be(user.Id.ToString());
        result.Claims[JwtRegisteredClaimNames.Email].Should().Be(user.Email);
    }

    /// <summary>
    /// The decision from ADR-006, asserted rather than only written down.
    /// </summary>
    /// <remarks>
    /// If workspace membership ever leaks into the token, removing someone from a workspace stops
    /// taking effect until their token expires. This test is what would catch that change.
    /// </remarks>
    [Fact]
    public void An_access_token_carries_no_workspace_claims()
    {
        var service = NewService(new FixedClock(Now));

        var token = service.CreateAccessToken(NewUser());
        var payload = new JsonWebToken(token);

        payload.Claims.Select(c => c.Type)
            .Should().NotContain(type => type.Contains("workspace", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task A_token_signed_with_a_different_key_does_not_validate()
    {
        var service = NewService(new FixedClock(Now));
        var token = service.CreateAccessToken(NewUser());

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes("a-completely-different-key-also-32-chars")),
            ValidateLifetime = false,
        });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Refresh_tokens_are_unpredictable_and_stored_only_as_a_hash()
    {
        var service = NewService(new FixedClock(Now));

        var (first, firstHash) = service.CreateRefreshToken();
        var (second, _) = service.CreateRefreshToken();

        first.Should().NotBe(second);
        firstHash.Should().NotBe(first, "the stored value must not be the token itself");
        firstHash.Should().HaveLength(64).And.MatchRegex("^[0-9a-f]+$", "SHA-256 as lowercase hex");
        service.HashRefreshToken(first).Should().Be(firstHash, "hashing must be deterministic to look a token up");
    }

    [Fact]
    public void Lifetimes_come_from_configuration()
    {
        var service = NewService(new FixedClock(Now));

        service.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(15));
        service.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(14));
    }
}

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_token_is_usable_until_it_expires()
    {
        var user = User.Register("ada@workboard.dev", "Ada", "hash");
        var token = user.IssueRefreshToken("hash-value", Now.AddDays(14), Now);

        token.IsUsable(Now).Should().BeTrue();
        token.IsUsable(Now.AddDays(15)).Should().BeFalse();
    }

    [Fact]
    public void Rotating_a_token_revokes_it_and_records_its_successor()
    {
        var user = User.Register("ada@workboard.dev", "Ada", "hash");
        var token = user.IssueRefreshToken("original", Now.AddDays(14), Now);

        token.RotateTo("replacement", Now);

        token.IsActive.Should().BeFalse();
        token.ReplacedByTokenHash.Should().Be("replacement");
        token.RevokedReason.Should().Be("rotated");
    }

    [Fact]
    public void Changing_a_password_revokes_every_live_session()
    {
        var user = User.Register("ada@workboard.dev", "Ada", "hash");
        user.IssueRefreshToken("one", Now.AddDays(14), Now);
        user.IssueRefreshToken("two", Now.AddDays(14), Now);

        user.ChangePassword("new-hash");

        user.RefreshTokens.Should().OnlyContain(t => !t.IsActive,
            "a password change is usually a response to compromise; leaving sessions alive defeats it");
    }

    [Fact]
    public void A_deactivated_account_cannot_be_issued_new_tokens()
    {
        var user = User.Register("ada@workboard.dev", "Ada", "hash");
        user.Deactivate();

        var act = () => user.IssueRefreshToken("one", Now.AddDays(14), Now);

        act.Should().Throw<WorkBoard.Domain.Exceptions.DomainException>();
    }
}
