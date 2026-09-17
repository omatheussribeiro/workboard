using FluentValidation;
using WorkBoard.Application.DTOs.Projects;

namespace WorkBoard.Application.Validators.Projects;

/// <summary>Shape rules for <see cref="CreateProjectRequest"/>.</summary>
public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    /// <summary>Builds the rule set.</summary>
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);

        // Letters only, 2 to 10. The key is concatenated with a number to form "WB-42", so anything
        // containing a digit or a hyphen would make that key ambiguous to parse back.
        RuleFor(x => x.Key)
            .NotEmpty()
            .Length(2, 10)
            .Matches("^[A-Za-z]+$")
            .WithMessage("Project key must contain letters only.");

        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
