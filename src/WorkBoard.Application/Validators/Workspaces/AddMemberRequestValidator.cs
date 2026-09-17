using FluentValidation;
using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.Validators.Workspaces;

/// <summary>Shape rules for <see cref="AddMemberRequest"/>.</summary>
public sealed class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
{
    /// <summary>Builds the rule set.</summary>
    public AddMemberRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

        // Owner is excluded here as well as in the aggregate. A workspace has exactly one owner,
        // established at creation, and transferring it is a separate operation with its own rules.
        RuleFor(x => x.Role).IsInEnum().NotEqual(WorkspaceRole.Owner);
    }
}
