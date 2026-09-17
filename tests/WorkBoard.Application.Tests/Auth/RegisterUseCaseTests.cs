using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.UseCases.Auth.Register;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Tests.Auth;

/// <summary>Account creation.</summary>
public sealed class RegisterUseCaseTests
{
    private readonly AuthUseCaseFixture _fixture = new();

    private RegisterUseCase CreateUseCase() => new(
        _fixture.Users,
        _fixture.TokenIssuer,
        _fixture.PasswordHasher,
        NullLogger<RegisterUseCase>.Instance);

    [Fact]
    public async Task Rejects_an_address_that_is_already_registered()
    {
        _fixture.Users.EmailExistsAsync("taken@workboard.test", Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateUseCase()
            .ExecuteAsync(new RegisterRequest("Taken@WorkBoard.test", "Taken", "a-password-123"), default);

        await act.Should().ThrowAsync<ConflictException>();
        _fixture.Users.DidNotReceive().Add(Arg.Any<User>());
    }

    /// <summary>
    /// The address is normalised before it is stored, not only before it is compared.
    /// </summary>
    /// <remarks>
    /// Otherwise "Ada@…" and "ada@…" are two accounts that render identically everywhere, and the
    /// uniqueness check passes for both.
    /// </remarks>
    [Fact]
    public async Task Normalises_the_address_before_storing_it()
    {
        _fixture.PasswordHasher.Hash(Arg.Any<string>()).Returns("hashed");

        var response = await CreateUseCase()
            .ExecuteAsync(new RegisterRequest("  Mixed.Case@WorkBoard.TEST ", "Mixed", "a-password-123"), default);

        response.User.Email.Should().Be("mixed.case@workboard.test");
        _fixture.Users.Received(1).Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Stores_a_hash_rather_than_the_password()
    {
        _fixture.PasswordHasher.Hash("a-password-123").Returns("hashed");

        await CreateUseCase()
            .ExecuteAsync(new RegisterRequest("new@workboard.test", "New", "a-password-123"), default);

        _fixture.PasswordHasher.Received(1).Hash("a-password-123");
        _fixture.Users.Received(1).Add(Arg.Is<User>(user => user.PasswordHash == "hashed"));
    }
}
