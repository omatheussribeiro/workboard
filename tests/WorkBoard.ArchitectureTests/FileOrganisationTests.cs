namespace WorkBoard.ArchitectureTests;

/// <summary>
/// One public type per file.
/// </summary>
/// <remarks>
/// <para>
/// Enforced rather than agreed, because the drift is always reasonable in the moment: a DTO is two
/// lines, a validator belongs next to the thing it validates, an enum is only used here. The result
/// was a <c>TaskContracts.cs</c> holding fourteen types, where the only way to find
/// <c>MoveTaskRequest</c> was full-text search.
/// </para>
/// <para>
/// Nested types are exempt, as are the generated migrations, and a file may declare additional
/// non-public types — an internal log class next to the class that logs is not what this is about.
/// </para>
/// </remarks>
public sealed class FileOrganisationTests
{
    [Fact]
    public void Each_source_file_declares_one_public_type()
    {
        var offenders = SourceFileScanner.FilesWithMultiplePublicTypes().ToList();

        offenders.Should().BeEmpty(
            "a file named after the type it contains is the cheapest navigation the codebase has");
    }

    [Fact]
    public void Each_source_file_is_named_after_the_type_it_declares()
    {
        var offenders = SourceFileScanner.FilesNotNamedAfterTheirType().ToList();

        offenders.Should().BeEmpty("a type that is not where its name says it is has to be searched for");
    }
}
