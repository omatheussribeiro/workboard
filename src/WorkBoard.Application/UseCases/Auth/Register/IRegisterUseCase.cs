using WorkBoard.Application.DTOs.Auth;

namespace WorkBoard.Application.UseCases.Auth.Register;

/// <summary>Creates an account and signs it in.</summary>
public interface IRegisterUseCase
{
    /// <summary>Registers the account and returns a token pair.</summary>
    Task<AuthResponse> ExecuteAsync(RegisterRequest request, CancellationToken cancellationToken);
}
