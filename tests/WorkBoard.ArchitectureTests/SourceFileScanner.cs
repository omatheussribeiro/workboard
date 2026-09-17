using System.Text.RegularExpressions;

namespace WorkBoard.ArchitectureTests;

/// <summary>
/// Reads the files under <c>src/</c> to check how types are laid out across them.
/// </summary>
/// <remarks>
/// Source text rather than reflection, because the question is about files and reflection has no
/// concept of one. The pattern deliberately matches only top-level declarations — those starting at
/// column zero — so nested types and members do not count, and comments are stripped first so a
/// declaration quoted in prose does not.
/// </remarks>
internal static partial class SourceFileScanner
{
    /// <summary>Files declaring more than one top-level public type.</summary>
    public static IEnumerable<string> FilesWithMultiplePublicTypes()
    {
        foreach (var (path, declarations) in PublicDeclarations())
        {
            if (declarations.Count > 1)
            {
                yield return $"{path} declares {string.Join(", ", declarations)}";
            }
        }
    }

    /// <summary>Files whose single public type does not match the file name.</summary>
    public static IEnumerable<string> FilesNotNamedAfterTheirType()
    {
        foreach (var (path, declarations) in PublicDeclarations())
        {
            if (declarations.Count != 1)
            {
                continue;
            }

            var expected = Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(declarations[0], expected, StringComparison.Ordinal))
            {
                yield return $"{path} declares {declarations[0]}";
            }
        }
    }

    private static IEnumerable<(string Path, IReadOnlyList<string> Declarations)> PublicDeclarations()
    {
        var repositoryRoot = RepositoryRoot.Find();

        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(repositoryRoot, "src"),
                     "*.cs",
                     SearchOption.AllDirectories))
        {
            if (IsGenerated(file))
            {
                continue;
            }

            var code = StripComments(File.ReadAllText(file));

            var declarations = PublicTypePattern()
                .Matches(code)
                .Select(match => match.Groups["name"].Value)
                .ToList();

            if (declarations.Count > 0)
            {
                yield return (
                    Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'),
                    declarations);
            }
        }
    }

    private static bool IsGenerated(string file) =>
        file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)

        // Top-level statements: the compiler generates the containing class, and the partial Program
        // declaration exists only so the test host can see it.
        || Path.GetFileName(file) == "Program.cs";

    private static string StripComments(string code) =>
        BlockCommentPattern().Replace(LineCommentPattern().Replace(code, string.Empty), string.Empty);

    [GeneratedRegex(
        @"^public\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+)*(?:class|record|interface|enum|struct)\s+(?<name>\w+)",
        RegexOptions.Multiline)]
    private static partial Regex PublicTypePattern();

    [GeneratedRegex(@"//.*$", RegexOptions.Multiline)]
    private static partial Regex LineCommentPattern();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockCommentPattern();
}
