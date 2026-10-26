using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Search;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

public sealed class SearchQueries(WorkBoardDbContext context) : ISearchQueries
{
    private const int PerTypeLimit = 8;
    private const int SnippetLength = 120;

    public async Task<SearchResultsDto> SearchAsync(string term, CancellationToken cancellationToken) =>
        new(
            await SearchTasksAsync(term, cancellationToken),
            await SearchProjectsAsync(term, cancellationToken),
            await SearchCommentsAsync(term, cancellationToken));

    private async Task<IReadOnlyList<SearchHitDto>> SearchTasksAsync(
        string query,
        CancellationToken cancellationToken) =>
        await context.Tasks
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
        await context.Projects
            .AsNoTracking()
            .Where(project => !project.IsArchived && (project.Name.Contains(query) || project.Key.Contains(query)))
            .OrderBy(project => project.Name)
            .Take(PerTypeLimit)
            .Select(project => new SearchHitDto(project.Id, project.Name, project.Key, null, project.Id))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<SearchHitDto>> SearchCommentsAsync(
        string query,
        CancellationToken cancellationToken) =>
        await context.Comments
            .AsNoTracking()
            .Where(comment => !comment.IsDeleted && comment.Body.Contains(query))
            .OrderByDescending(comment => comment.CreatedAtUtc)
            .Take(PerTypeLimit)
            .Select(comment => new SearchHitDto(
                comment.Id,
                comment.Task.Title,
                comment.Body.Length > SnippetLength ? comment.Body.Substring(0, SnippetLength) : comment.Body,
                comment.TaskId,
                comment.Task.ProjectId))
            .ToListAsync(cancellationToken);
}
