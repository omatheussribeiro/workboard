using WorkBoard.Application.DTOs.Workspaces;

namespace WorkBoard.Application.UseCases.Workspaces.CreateWorkspace;

/// <summary>Creates a workspace — and therefore a tenant — owned by the caller.</summary>
public interface ICreateWorkspaceUseCase
{
    /// <summary>Creates it, or reports a conflict when the slug is taken.</summary>
    Task<WorkspaceSummaryDto> ExecuteAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken);
}
