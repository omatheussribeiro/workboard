using FluentValidation;
using WorkBoard.Application.DTOs.Auth;

namespace WorkBoard.Application.Validators.Auth;

/// <summary>Shape rules for <see cref="RefreshRequest"/>.</summary>
public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    /// <summary>Builds the rule set.</summary>
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
}
