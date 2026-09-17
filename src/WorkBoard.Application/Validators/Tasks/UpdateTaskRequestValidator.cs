using FluentValidation;
using WorkBoard.Application.DTOs.Tasks;

namespace WorkBoard.Application.Validators.Tasks;

/// <summary>Shape rules for <see cref="UpdateTaskRequest"/>.</summary>
public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    /// <summary>Builds the rule set.</summary>
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(10_000);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("A concurrency token is required.");
    }
}
