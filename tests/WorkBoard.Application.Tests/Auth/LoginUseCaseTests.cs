using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.UseCases.Auth.Login;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Tests.Auth;

/// <summary>Credential verification, including the parts that have no visible output.</summary>
public sealed class LoginUseCaseTests
{
    private readonly AuthUseCaseFixture _fixture = new();

    private LoginUseCase CreateUseCase() => new(
        _fixture.Users,
        _fixture.TokenIssuer,
        _fixture.PasswordHasher,
        NullLogger<LoginUseCase>.Instance);

    /// <summary>
    /// A missing account must cost the same as a wrong password.
    /// </summary>
    /// <remarks>
    /// BCrypt at cost 12 takes a few hundred milliseconds. Skipping it when the account does not
    /// exist would make "unknown address" measurably faster than "wrong password" over the network,
    /// which is a working account-enumeration oracle regardless of what the response body says. The
    /// assertion is on the hasher being called, because that is the observable part of the defence —
    /// and the reason it is worth a test is that the line looks like dead code to a reader.
    /// </remarks>
    [Fact]
    public async Task Still_verifies_a_hash_when_the_account_does_not_exist()
    {
        _fixture.Users.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => CreateUseCase().ExecuteAsync(new LoginRequest("ghost@workboard.test", "whatever"), default);

        await act.Should().ThrowAsync<ForbiddenException>();
        _fixture.PasswordHasher.Received(1).Verify("whatever", Arg.Any<string>());
    }

    [Fact]
    public async Task Rejects_a_wrong_password_with_the_same_message_as_a_missing_account()
    {
        _fixture.Users.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthUseCaseFixture.NewUser());
        _fixture.PasswordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var act = () => CreateUseCase().ExecuteAsync(new LoginRequest("someone@workboard.test", "wrong"), default);

        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task Refuses_a_deactivated_account_even_with_the_right_password()
    {
        var user = AuthUseCaseFixture.NewUser();
        user.Deactivate();

        _fixture.Users.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _fixture.PasswordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var act = () => CreateUseCase().ExecuteAsync(new LoginRequest("someone@workboard.test", "right"), default);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Issues_a_token_pair_for_valid_credentials()
    {
        _fixture.Users.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(AuthUseCaseFixture.NewUser());
        _fixture.PasswordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var response = await CreateUseCase()
            .ExecuteAsync(new LoginRequest("someone@workboard.test", "right"), default);

        response.AccessToken.Should().Be("access-token");
        response.RefreshToken.Should().Be("refresh-token");
    }
}
