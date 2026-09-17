namespace WorkBoard.ArchitectureTests;

/// <summary>
/// Locates the checkout, for the tests that inspect source files rather than compiled types.
/// </summary>
/// <remarks>
/// Those tests are meaningful in CI and in a developer checkout, and would silently pass if the
/// sources were absent — so this throws rather than returning a path that does not exist.
/// </remarks>
internal static class RepositoryRoot
{
    /// <summary>Walks up from the test binaries until it finds the solution file.</summary>
    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "WorkBoard.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new InvalidOperationException(
                   "Could not locate the repository root. This test inspects source files and needs the checkout present.");
    }
}
