using WorkBoard.Application.DTOs.Workspaces;

namespace WorkBoard.Application.UseCases.Workspaces.GetWorkspace;

/// <summary>Reads one workspace the caller belongs to.</summary>
public interface IGetWorkspaceUseCase
{
    /// <summary>Returns the workspace.</summary>
    /// <exception cref="WorkBoard.Application.Exceptions.NotFoundException">Unknown workspace, or the caller is not a member.</exception>
    Task<WorkspaceSummaryDto> ExecuteAsync(Guid workspaceId, CancellationToken cancellationToken);
}
