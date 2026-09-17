using Microsoft.EntityFrameworkCore;
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
public sealed class SearchWorkspaceUseCase(IWorkBoardDbContext db) : ISearchWorkspaceUseCase
{
    private const int PerTypeLimit = 8;
    private const int MinimumTermLength = 2;
    private const int SnippetLength = 120;

    /// <inheritdoc/>
    public async Task<SearchResultsDto> ExecuteAsync(string term, CancellationToken cancellationToken)
    {
        var query = term.Trim();

        // A one-character term matches most of the workspace and costs a full scan to say so.
        if (query.Length < MinimumTermLength)
        {
            return new SearchResultsDto([], [], []);
        }

        return new SearchResultsDto(
            await SearchTasksAsync(query, cancellationToken),
            await SearchProjectsAsync(query, cancellationToken),
            await SearchCommentsAsync(query, cancellationToken));
    }

    private async Task<IReadOnlyList<SearchHitDto>> SearchTasksAsync(
        string query,
        CancellationToken cancellationToken) =>
        await db.Tasks
            .AsNoTracking()
            .Where(task => task.Title.Contains(query))
            .OrderByDescending(task => task.CreatedAtUtc)
            .Take(PerTypeLimit)
            .Select(task => new SearchHitDto(
                task.Id,
                task.Title,
                task.Project.Key + "-" + task.Number,
                task.Id,
                task.ProjectId))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<SearchHitDto>> SearchProjectsAsync(
        string query,
        CancellationToken cancellationToken) =>
        await db.Projects
            .AsNoTracking()
            .Where(project => !project.IsArchived && (project.Name.Contains(query) || project.Key.Contains(query)))
            .OrderBy(project => project.Name)
            .Take(PerTypeLimit)
            .Select(project => new SearchHitDto(project.Id, project.Name, project.Key, null, project.Id))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<SearchHitDto>> SearchCommentsAsync(
        string query,
        CancellationToken cancellationToken) =>
        await db.Comments
            .AsNoTracking()
            .Where(comment => !comment.IsDeleted && comment.Body.Contains(query))
            .OrderByDescending(comment => comment.CreatedAtUtc)
            .Take(PerTypeLimit)
            .Select(comment => new SearchHitDto(
                comment.Id,
                comment.Task.Title,

                // Truncated in SQL rather than after loading: comment bodies run to 5 KB and the
                // palette shows one line of each.
                comment.Body.Length > SnippetLength ? comment.Body.Substring(0, SnippetLength) : comment.Body,
                comment.TaskId,
                comment.Task.ProjectId))
            .ToListAsync(cancellationToken);
}
