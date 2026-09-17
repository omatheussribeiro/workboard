namespace WorkBoard.Application.DTOs.Auth;

/// <summary>Exchanges a refresh token for a new pair.</summary>
/// <param name="RefreshToken">The opaque token handed out by the previous login or refresh.</param>
public sealed record RefreshRequest(string RefreshToken);
