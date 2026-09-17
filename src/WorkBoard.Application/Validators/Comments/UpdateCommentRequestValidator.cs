using FluentValidation;
using WorkBoard.Application.DTOs.Comments;

namespace WorkBoard.Application.Validators.Comments;

/// <summary>Shape rules for <see cref="UpdateCommentRequest"/>.</summary>
public sealed class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
{
    /// <summary>Builds the rule set.</summary>
    public UpdateCommentRequestValidator() => RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
}
