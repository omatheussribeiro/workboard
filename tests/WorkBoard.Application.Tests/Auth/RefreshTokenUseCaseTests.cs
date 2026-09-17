using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.UseCases.Auth.RefreshToken;

namespace WorkBoard.Application.Tests.Auth;

/// <summary>Rotation, and what happens when a rotated token comes back.</summary>
public sealed class RefreshTokenUseCaseTests
{
    private readonly AuthUseCaseFixture _fixture = new();

    private RefreshTokenUseCase CreateUseCase() => new(
        _fixture.Users,
        _fixture.TokenIssuer,
        _fixture.Tokens,
        _fixture.UnitOfWork,
        _fixture.Clock,
        NullLogger<RefreshTokenUseCase>.Instance);

    /// <summary>
    /// Replaying a rotated token revokes the whole chain.
    /// </summary>
    /// <remarks>
    /// A token presented after it was rotated means either a replay or a theft, and the two are
    /// indistinguishable from the server. Revoking everything makes an attacker's success visible —
    /// the legitimate user is logged out — rather than granting a quiet parallel session.
    /// </remarks>
    [Fact]
    public async Task Revokes_every_session_when_a_rotated_token_is_presented_again()
    {
        var user = AuthUseCaseFixture.NewUser();
        var issued = user.IssueRefreshToken("original-hash", _fixture.Clock.UtcNow.AddDays(14), _fixture.Clock.UtcNow);
        issued.RotateTo("replacement-hash", _fixture.Clock.UtcNow);

        var stillActive = user.IssueRefreshToken(
            "another-hash",
            _fixture.Clock.UtcNow.AddDays(14),
            _fixture.Clock.UtcNow);

        _fixture.Tokens.HashRefreshToken("replayed").Returns("original-hash");
        _fixture.Users.FindRefreshTokenAsync("original-hash", Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenOwner(issued, user));
        _fixture.Users.ListActiveRefreshTokensAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([stillActive]);

        var act = () => CreateUseCase().ExecuteAsync(new RefreshRequest("replayed"), default);

        await act.Should().ThrowAsync<ForbiddenException>();
        stillActive.IsActive.Should().BeFalse("a replay means the chain is compromised, not just that one token");
    }

    [Fact]
    public async Task Rotates_a_usable_token()
    {
        var user = AuthUseCaseFixture.NewUser();
        var issued = user.IssueRefreshToken("current-hash", _fixture.Clock.UtcNow.AddDays(14), _fixture.Clock.UtcNow);

        _fixture.Tokens.HashRefreshToken("current").Returns("current-hash");
        _fixture.Users.FindRefreshTokenAsync("current-hash", Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenOwner(issued, user));

        var response = await CreateUseCase().ExecuteAsync(new RefreshRequest("current"), default);

        response.RefreshToken.Should().Be("refresh-token");
        issued.IsActive.Should().BeFalse("the presented token is consumed by the rotation");
        await _fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_an_unknown_token_with_the_same_message_as_a_revoked_one()
    {
        _fixture.Users.FindRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshTokenOwner?)null);

        var act = () => CreateUseCase().ExecuteAsync(new RefreshRequest("never-issued"), default);

        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage("Invalid refresh token.");
    }
}
