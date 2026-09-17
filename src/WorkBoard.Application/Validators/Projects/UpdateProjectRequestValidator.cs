using FluentValidation;
using WorkBoard.Application.DTOs.Projects;

namespace WorkBoard.Application.Validators.Projects;

/// <summary>Shape rules for <see cref="UpdateProjectRequest"/>.</summary>
public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    /// <summary>Builds the rule set.</summary>
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
