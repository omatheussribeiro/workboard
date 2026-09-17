namespace WorkBoard.Application.Validators.Labels;

/// <summary>
/// Rules shared by the create and update validators.
/// </summary>
/// <remarks>
/// The two validators previously carried the same regular expression, and only one of them carried
/// the message that explains it — so creating a label with a bad colour was a sentence and renaming
/// one to the same bad colour was FluentValidation's default text. One constant removes both the
/// duplication and the divergence.
/// </remarks>
internal static class LabelValidationRules
{
    /// <summary>Six hex digits, with an optional leading hash.</summary>
    internal const string ColorPattern = "^#?[0-9a-fA-F]{6}$";

    /// <summary>Message shown when <see cref="ColorPattern"/> does not match.</summary>
    internal const string ColorMessage = "Colour must be a 6-digit hex value.";
}
