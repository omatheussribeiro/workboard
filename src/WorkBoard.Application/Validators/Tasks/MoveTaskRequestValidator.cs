using FluentValidation;
using WorkBoard.Application.DTOs.Tasks;

namespace WorkBoard.Application.Validators.Tasks;

/// <summary>Shape rules for <see cref="MoveTaskRequest"/>.</summary>
public sealed class MoveTaskRequestValidator : AbstractValidator<MoveTaskRequest>
{
    /// <summary>Builds the rule set.</summary>
    public MoveTaskRequestValidator()
    {
        RuleFor(x => x.TargetColumnId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("A concurrency token is required.");
        RuleFor(x => x).Must(x => x.PreviousTaskId != x.NextTaskId || x.PreviousTaskId is null)
            .WithMessage("Neighbour tasks must be different.");
    }
}
