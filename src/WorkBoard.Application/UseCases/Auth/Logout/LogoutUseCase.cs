using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Auth.Logout;

/// <inheritdoc cref="ILogoutUseCase"/>
public sealed class LogoutUseCase(
    IUserRepository users,
    ITokenService tokens,
    IUnitOfWork unitOfWork,
    IClock clock) : ILogoutUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await users.FindRefreshTokenAsync(hash, cancellationToken);

        // Logging out an unknown or already-revoked token is not an error: the client wanted the
        // session gone and it is gone. Reporting a failure would only invite probing.
        stored?.Token.Revoke("logout", clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
