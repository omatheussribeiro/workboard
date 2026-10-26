using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Projects;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Projects;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

public sealed class ProjectQueries(WorkBoardDbContext context) : IProjectQueries
{
    public Task<ProjectSummaryDto?> GetAsync(Guid projectId, CancellationToken cancellationToken) =>
        context.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(ProjectMappings.ToSummary())
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectSummaryDto>> ListAsync(
        bool includeArchived,
        CancellationToken cancellationToken) =>
        await context.Projects
            .AsNoTracking()
            .WhereIf(!includeArchived, project => !project.IsArchived)
            .OrderBy(project => project.Name)
            .Select(ProjectMappings.ToSummary())
            .ToListAsync(cancellationToken);
}
