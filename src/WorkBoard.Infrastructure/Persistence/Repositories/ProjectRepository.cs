using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IProjectRepository"/>
public sealed class ProjectRepository(WorkBoardDbContext context) : IProjectRepository
{
    /// <inheritdoc/>
    public Task<Project?> FindForUpdateAsync(Guid projectId, CancellationToken cancellationToken) =>
        context.Projects.FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);

    /// <inheritdoc/>
    public Task<Project?> FindWithBoardsAsync(Guid projectId, CancellationToken cancellationToken) =>
        context.Projects
            .Include(project => project.Boards)
            .FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken) =>
        context.Projects.AnyAsync(project => project.Key == key, cancellationToken);

    /// <inheritdoc/>
    public void Add(Project project) => context.Projects.Add(project);
}
