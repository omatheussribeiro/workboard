using WorkBoard.Application.DTOs.Projects;

namespace WorkBoard.Application.Interfaces;

public interface IProjectQueries
{
    Task<ProjectSummaryDto?> GetAsync(Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectSummaryDto>> ListAsync(bool includeArchived, CancellationToken cancellationToken);
}
