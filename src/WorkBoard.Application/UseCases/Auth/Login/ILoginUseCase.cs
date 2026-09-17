using WorkBoard.Application.DTOs.Auth;

namespace WorkBoard.Application.UseCases.Auth.Login;

/// <summary>Verifies credentials and issues a token pair.</summary>
public interface ILoginUseCase
{
    /// <summary>Signs the user in.</summary>
    Task<AuthResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken);
}
