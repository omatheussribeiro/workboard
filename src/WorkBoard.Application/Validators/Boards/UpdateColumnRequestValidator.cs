using FluentValidation;
using WorkBoard.Application.DTOs.Boards;

namespace WorkBoard.Application.Validators.Boards;

/// <summary>Shape rules for <see cref="UpdateColumnRequest"/>.</summary>
public sealed class UpdateColumnRequestValidator : AbstractValidator<UpdateColumnRequest>
{
    /// <summary>Builds the rule set.</summary>
    public UpdateColumnRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.WipLimit).GreaterThan(0).When(x => x.WipLimit.HasValue);
    }
}
