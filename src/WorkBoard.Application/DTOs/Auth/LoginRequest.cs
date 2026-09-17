namespace WorkBoard.Application.DTOs.Auth;

/// <summary>Credentials presented at the login endpoint.</summary>
/// <param name="Email">Login address, matched case-insensitively.</param>
/// <param name="Password">Plain-text password.</param>
public sealed record LoginRequest(string Email, string Password);
