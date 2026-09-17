using WorkBoard.Application.DTOs.Auth;

namespace WorkBoard.Application.UseCases.Auth.RefreshToken;

/// <summary>Rotates a refresh token and issues a new pair.</summary>
public interface IRefreshTokenUseCase
{
    /// <summary>Exchanges the presented token for a new pair.</summary>
    Task<AuthResponse> ExecuteAsync(RefreshRequest request, CancellationToken cancellationToken);
}
