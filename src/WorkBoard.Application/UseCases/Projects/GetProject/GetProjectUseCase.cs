using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Projects;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Projects;

namespace WorkBoard.Application.UseCases.Projects.GetProject;

/// <inheritdoc cref="IGetProjectUseCase"/>
public sealed class GetProjectUseCase(IWorkBoardDbContext db) : IGetProjectUseCase
{
    /// <inheritdoc/>
    public async Task<ProjectSummaryDto> ExecuteAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(ProjectMappings.ToSummary())
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Project", projectId);
}
