namespace WorkBoard.Application.UseCases.Workspaces.ArchiveWorkspace;

/// <summary>Archives a workspace. Only the owner may do this.</summary>
public interface IArchiveWorkspaceUseCase
{
    /// <summary>Archives it. Nothing is deleted.</summary>
    Task ExecuteAsync(Guid workspaceId, CancellationToken cancellationToken);
}
