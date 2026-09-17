using System.Text.RegularExpressions;

namespace WorkBoard.ArchitectureTests;

/// <summary>
/// Finds every file in <c>src/</c> that bypasses the workspace query filter.
/// </summary>
/// <remarks>
/// <para>
/// This reads source files rather than inspecting IL, which deserves an explanation because it is
/// the unusual choice. Detecting the call in compiled code means either parsing IL by hand — a
/// variable-length instruction stream, easy to get subtly wrong — or taking a dependency on Cecil
/// to answer one question. Source text is unambiguous here: both method names are distinctive
/// enough that a substring match has no false positives, and comments are stripped first so a
/// mention in prose does not trip it.
/// </para>
/// <para>
/// Results are reported as repository-relative file paths rather than type names. A file can declare
/// several types, so naming the type would mean deciding which one the call belongs to — and the
/// allow-list is easier to review as a list of files anyway.
/// </para>
/// <para>
/// The trade-off is that the test needs the repository on disk, so it is meaningful in CI and in a
/// developer checkout, and would silently pass if the sources were absent. <see cref="RepositoryRoot"/>
/// therefore fails loudly when it cannot find them rather than returning an empty result.
/// </para>
/// </remarks>
internal static partial class FilterBypassScanner
{
    private static readonly string[] BypassCalls = ["IgnoringTenantFilter", "IgnoreQueryFilters"];

    public static IEnumerable<string> FindCallers()
    {
        var repositoryRoot = RepositoryRoot.Find();
        var sourceRoot = Path.Combine(repositoryRoot, "src");

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            // Generated migrations never touch query filters, and scanning them is slow.
            if (file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var code = StripComments(File.ReadAllText(file));
            if (!BypassCalls.Any(call => code.Contains(call + "<", StringComparison.Ordinal)
                                         || code.Contains(call + "(", StringComparison.Ordinal)))
            {
                continue;
            }

            // Normalised to forward slashes so the allow-list reads the same on Windows and Linux.
            yield return Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/');
        }
    }

    private static string StripComments(string code) =>
        BlockCommentPattern().Replace(LineCommentPattern().Replace(code, string.Empty), string.Empty);

    [GeneratedRegex(@"//.*$", RegexOptions.Multiline)]
    private static partial Regex LineCommentPattern();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockCommentPattern();
}
