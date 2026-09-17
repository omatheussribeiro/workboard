using WorkBoard.Application.DTOs.Search;

namespace WorkBoard.Application.UseCases.Search.SearchWorkspace;

/// <summary>The command-palette search across cards, projects and comments.</summary>
public interface ISearchWorkspaceUseCase
{
    /// <summary>Searches the current workspace. Terms shorter than two characters return nothing.</summary>
    Task<SearchResultsDto> ExecuteAsync(string term, CancellationToken cancellationToken);
}
