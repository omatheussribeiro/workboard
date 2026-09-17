using WorkBoard.Domain.Interfaces;

namespace WorkBoard.Domain.Events;

/// <summary>Raised when an account is created.</summary>
/// <param name="UserId">The new account.</param>
/// <param name="Email">Its address.</param>
public sealed record UserRegistered(Guid UserId, string Email) : IDomainEvent;
