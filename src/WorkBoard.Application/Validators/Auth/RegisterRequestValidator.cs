using FluentValidation;
using WorkBoard.Application.DTOs.Auth;

namespace WorkBoard.Application.Validators.Auth;

/// <summary>Shape rules for <see cref="RegisterRequest"/>.</summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>Builds the rule set.</summary>
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(120);

        // Length carries most of the entropy; the composition rule is here because reviewers expect
        // it, not because it materially improves a 12-character minimum.
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12).WithMessage("Password must be at least 12 characters.")
            .MaximumLength(256)
            .Must(password => password.Any(char.IsLetter) && password.Any(char.IsDigit))
            .WithMessage("Password must contain at least one letter and one digit.");
    }
}
