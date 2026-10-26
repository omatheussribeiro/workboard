using WorkBoard.Application.DTOs.Search;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Search.SearchWorkspace;

/// <inheritdoc cref="ISearchWorkspaceUseCase"/>
/// <remarks>
/// <para>
/// Three narrow queries rather than one union. A union over differently shaped tables forces a
/// lowest-common-denominator projection and gives SQL Server a plan it cannot index well; three
/// small top-N queries each hit their own index, and the client wants them grouped by type anyway.
/// </para>
/// <para>
/// Every query is capped with <c>Take</c>. A search box has no pagination, so an uncapped query is a
/// way for one keystroke to return the whole workspace.
/// </para>
/// </remarks>
public sealed class SearchWorkspaceUseCase(ISearchQueries queries) : ISearchWorkspaceUseCase
{
    private const int MinimumTermLength = 2;

    /// <inheritdoc/>
    public async Task<SearchResultsDto> ExecuteAsync(string term, CancellationToken cancellationToken)
    {
        var query = term.Trim();

        // A one-character term matches most of the workspace and costs a full scan to say so.
        if (query.Length < MinimumTermLength)
        {
            return new SearchResultsDto([], [], []);
        }

        return await queries.SearchAsync(query, cancellationToken);
    }

}
