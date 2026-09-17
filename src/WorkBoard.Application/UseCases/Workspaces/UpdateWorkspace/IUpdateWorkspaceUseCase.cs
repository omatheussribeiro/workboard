using WorkBoard.Application.DTOs.Workspaces;

namespace WorkBoard.Application.UseCases.Workspaces.UpdateWorkspace;

/// <summary>Renames a workspace and updates its description.</summary>
public interface IUpdateWorkspaceUseCase
{
    /// <summary>Applies the change. The slug is not editable: it is a URL.</summary>
    Task ExecuteAsync(Guid workspaceId, UpdateWorkspaceRequest request, CancellationToken cancellationToken);
}
