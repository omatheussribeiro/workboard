using FluentValidation;
using WorkBoard.Application.DTOs.Labels;

namespace WorkBoard.Application.Validators.Labels;

/// <summary>Shape rules for <see cref="UpdateLabelRequest"/>.</summary>
public sealed class UpdateLabelRequestValidator : AbstractValidator<UpdateLabelRequest>
{
    /// <summary>Builds the rule set.</summary>
    public UpdateLabelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(40);
        RuleFor(x => x.ColorHex)
            .NotEmpty()
            .Matches(LabelValidationRules.ColorPattern)
            .WithMessage(LabelValidationRules.ColorMessage);
    }
}
