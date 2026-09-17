using FluentValidation;
using WorkBoard.Application.DTOs.Auth;

namespace WorkBoard.Application.Validators.Auth;

/// <summary>Shape rules for <see cref="LoginRequest"/>.</summary>
/// <remarks>
/// Deliberately weaker than <see cref="RegisterRequestValidator"/>: applying the registration
/// password policy at login would reject a stored credential that predates a policy change, and
/// would tell an attacker which submitted passwords are even worth trying.
/// </remarks>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Builds the rule set.</summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}
