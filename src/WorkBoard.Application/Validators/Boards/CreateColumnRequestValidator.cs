using FluentValidation;
using WorkBoard.Application.DTOs.Boards;

namespace WorkBoard.Application.Validators.Boards;

/// <summary>Shape rules for <see cref="CreateColumnRequest"/>.</summary>
public sealed class CreateColumnRequestValidator : AbstractValidator<CreateColumnRequest>
{
    /// <summary>Builds the rule set.</summary>
    public CreateColumnRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.WipLimit).GreaterThan(0).When(x => x.WipLimit.HasValue);
    }
}
