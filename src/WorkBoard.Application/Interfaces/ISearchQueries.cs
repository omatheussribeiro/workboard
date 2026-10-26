using WorkBoard.Application.DTOs.Search;

namespace WorkBoard.Application.Interfaces;

public interface ISearchQueries
{
    Task<SearchResultsDto> SearchAsync(string term, CancellationToken cancellationToken);
}
