using FluentValidation;
using WorkBoard.Application.DTOs.Tasks;

namespace WorkBoard.Application.Validators.Tasks;

/// <summary>Shape and size rules for <see cref="CreateTaskRequest"/>.</summary>
/// <remarks>
/// Only structural rules live here. Whether the column belongs to the project, and whether the
/// assignees are members of the workspace, are decided against the database by the command service —
/// a validator has no tenant and no transaction, so enforcing them here would be a check that is
/// already stale by the time the insert runs.
/// </remarks>
public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    /// <summary>Builds the rule set.</summary>
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.BoardColumnId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(10_000);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.AssigneeIds).Must(ids => ids is null || ids.Count <= 10)
            .WithMessage("A task can have at most 10 assignees.");
        RuleFor(x => x.LabelIds).Must(ids => ids is null || ids.Count <= 20)
            .WithMessage("A task can have at most 20 labels.");
    }
}
