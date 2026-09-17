using FluentValidation;
using WorkBoard.Application.DTOs.Comments;

namespace WorkBoard.Application.Validators.Comments;

/// <summary>Shape rules for <see cref="CreateCommentRequest"/>.</summary>
public sealed class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    /// <summary>Builds the rule set.</summary>
    public CreateCommentRequestValidator() => RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
}
