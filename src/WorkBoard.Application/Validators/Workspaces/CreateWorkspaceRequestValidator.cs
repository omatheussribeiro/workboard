using FluentValidation;
using WorkBoard.Application.DTOs.Workspaces;

namespace WorkBoard.Application.Validators.Workspaces;

/// <summary>Shape rules for <see cref="CreateWorkspaceRequest"/>.</summary>
public sealed class CreateWorkspaceRequestValidator : AbstractValidator<CreateWorkspaceRequest>
{
    /// <summary>Builds the rule set.</summary>
    public CreateWorkspaceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);

        // Constrained to a slug shape rather than escaped later. A value that appears in a URL and
        // is compared for uniqueness should have exactly one normal form, or "Acme" and "acme-"
        // become different tenants that look identical in the address bar.
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(60)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase letters, digits and single hyphens.");

        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
