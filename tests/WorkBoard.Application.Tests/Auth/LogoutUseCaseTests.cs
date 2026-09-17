using NSubstitute;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.UseCases.Auth.Logout;

namespace WorkBoard.Application.Tests.Auth;

/// <summary>Logging out is idempotent, deliberately.</summary>
public sealed class LogoutUseCaseTests
{
    private readonly AuthUseCaseFixture _fixture = new();

    private LogoutUseCase CreateUseCase() => new(
        _fixture.Users,
        _fixture.Tokens,
        _fixture.UnitOfWork,
        _fixture.Clock);

    [Fact]
    public async Task Treats_an_unknown_token_as_success()
    {
        _fixture.Users.FindRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshTokenOwner?)null);

        var act = () => CreateUseCase().ExecuteAsync("never-issued", default);

        await act.Should().NotThrowAsync("the client wanted the session gone and it is gone");
    }

    [Fact]
    public async Task Revokes_a_known_token()
    {
        var user = AuthUseCaseFixture.NewUser();
        var issued = user.IssueRefreshToken("hash", _fixture.Clock.UtcNow.AddDays(14), _fixture.Clock.UtcNow);

        _fixture.Tokens.HashRefreshToken("token").Returns("hash");
        _fixture.Users.FindRefreshTokenAsync("hash", Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenOwner(issued, user));

        await CreateUseCase().ExecuteAsync("token", default);

        issued.IsActive.Should().BeFalse();
        await _fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
