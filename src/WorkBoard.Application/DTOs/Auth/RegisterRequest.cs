namespace WorkBoard.Application.DTOs.Auth;

/// <summary>A new account.</summary>
/// <param name="Email">Login address; normalised to lower case before storage.</param>
/// <param name="DisplayName">Name shown on cards and comments.</param>
/// <param name="Password">Plain-text password; hashed with BCrypt and never persisted as given.</param>
public sealed record RegisterRequest(string Email, string DisplayName, string Password);
