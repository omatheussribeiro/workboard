using FluentValidation;
using WorkBoard.Application.DTOs.Labels;

namespace WorkBoard.Application.Validators.Labels;

/// <summary>Shape rules for <see cref="CreateLabelRequest"/>.</summary>
public sealed class CreateLabelRequestValidator : AbstractValidator<CreateLabelRequest>
{
    /// <summary>Builds the rule set.</summary>
    public CreateLabelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(40);
        RuleFor(x => x.ColorHex)
            .NotEmpty()
            .Matches(LabelValidationRules.ColorPattern)
            .WithMessage(LabelValidationRules.ColorMessage);
    }
}
