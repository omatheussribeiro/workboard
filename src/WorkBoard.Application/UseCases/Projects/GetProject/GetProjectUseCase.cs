using WorkBoard.Application.DTOs.Projects;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Projects.GetProject;

/// <inheritdoc cref="IGetProjectUseCase"/>
public sealed class GetProjectUseCase(IProjectQueries queries) : IGetProjectUseCase
{
    /// <inheritdoc/>
    public async Task<ProjectSummaryDto> ExecuteAsync(Guid projectId, CancellationToken cancellationToken) =>
        await queries.GetAsync(projectId, cancellationToken)
        ?? throw new NotFoundException("Project", projectId);
}
