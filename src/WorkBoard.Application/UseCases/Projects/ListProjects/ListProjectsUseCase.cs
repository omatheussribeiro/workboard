using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Projects;
using WorkBoard.Application.Extensions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Projects;

namespace WorkBoard.Application.UseCases.Projects.ListProjects;

/// <inheritdoc cref="IListProjectsUseCase"/>
/// <remarks>
/// Notice what is absent: the query does not mention <c>WorkspaceId</c>. The global query filter
/// already restricts every <c>DbSet</c> to the workspace resolved for the request, so a read cannot
/// leak another tenant's projects by forgetting a <c>Where</c>. Forgetting is the failure mode a
/// filter defends against; remembering is not something to rely on.
/// </remarks>
public sealed class ListProjectsUseCase(IWorkBoardDbContext db) : IListProjectsUseCase
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProjectSummaryDto>> ExecuteAsync(
        bool includeArchived,
        CancellationToken cancellationToken) =>
        await db.Projects
            .AsNoTracking()
            .WhereIf(!includeArchived, project => !project.IsArchived)
            .OrderBy(project => project.Name)
            .Select(ProjectMappings.ToSummary())
            .ToListAsync(cancellationToken);
}
