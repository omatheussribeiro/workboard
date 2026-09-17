using WorkBoard.Application.DTOs.Workspaces;

namespace WorkBoard.Application.UseCases.Workspaces.ListMyWorkspaces;

/// <summary>Lists the workspaces the caller belongs to.</summary>
public interface IListMyWorkspacesUseCase
{
    /// <summary>Every workspace the caller is a member of, ordered by name.</summary>
    Task<IReadOnlyList<WorkspaceSummaryDto>> ExecuteAsync(CancellationToken cancellationToken);
}
