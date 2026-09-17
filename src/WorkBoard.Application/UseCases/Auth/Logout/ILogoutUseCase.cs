namespace WorkBoard.Application.UseCases.Auth.Logout;

/// <summary>Revokes one refresh token.</summary>
public interface ILogoutUseCase
{
    /// <summary>Revokes it. An unknown token is not an error.</summary>
    Task ExecuteAsync(string refreshToken, CancellationToken cancellationToken);
}
