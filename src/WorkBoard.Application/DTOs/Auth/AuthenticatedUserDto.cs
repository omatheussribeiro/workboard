namespace WorkBoard.Application.DTOs.Auth;

/// <summary>The signed-in user, as the client needs them — no hash, no tokens, no audit fields.</summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">Login address.</param>
/// <param name="DisplayName">Name to render.</param>
/// <param name="IsSystemAdmin">Whether the platform-wide administration routes are available.</param>
public sealed record AuthenticatedUserDto(Guid Id, string Email, string DisplayName, bool IsSystemAdmin);
