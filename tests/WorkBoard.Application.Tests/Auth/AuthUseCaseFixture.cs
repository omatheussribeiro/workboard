using NSubstitute;
using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Services;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Tests.Auth;

/// <summary>Substitutes shared by the four authentication use cases.</summary>
internal sealed class AuthUseCaseFixture
{
    public IUserRepository Users { get; } = Substitute.For<IUserRepository>();

    public ITokenIssuer TokenIssuer { get; } = Substitute.For<ITokenIssuer>();

    public ITokenService Tokens { get; } = Substitute.For<ITokenService>();

    public IPasswordHasher PasswordHasher { get; } = Substitute.For<IPasswordHasher>();

    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    public TestClock Clock { get; } = new();

    public AuthUseCaseFixture()
    {
        Tokens.CreateRefreshToken().Returns(("refresh-token", "refresh-hash"));
        Tokens.RefreshTokenLifetime.Returns(TimeSpan.FromDays(14));
        Tokens.AccessTokenLifetime.Returns(TimeSpan.FromMinutes(15));

        TokenIssuer
            .IssueAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(call => Response(call.Arg<User>()));

        TokenIssuer
            .BuildResponse(Arg.Any<User>(), Arg.Any<string>())
            .Returns(call => Response(call.ArgAt<User>(0), call.ArgAt<string>(1)));
    }

    public static User NewUser(string email = "someone@workboard.test") =>
        User.Register(email, "Someone", "stored-hash");

    private AuthResponse Response(User user, string refreshToken = "refresh-token") =>
        new("access-token",
            refreshToken,
            Clock.UtcNow.AddMinutes(15),
            new AuthenticatedUserDto(user.Id, user.Email, user.DisplayName, user.IsSystemAdmin));
}
