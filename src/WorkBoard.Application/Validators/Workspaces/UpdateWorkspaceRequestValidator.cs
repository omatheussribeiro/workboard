using FluentValidation;
using WorkBoard.Application.DTOs.Workspaces;

namespace WorkBoard.Application.Validators.Workspaces;

/// <summary>Shape rules for <see cref="UpdateWorkspaceRequest"/>.</summary>
public sealed class UpdateWorkspaceRequestValidator : AbstractValidator<UpdateWorkspaceRequest>
{
    /// <summary>Builds the rule set.</summary>
    public UpdateWorkspaceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
