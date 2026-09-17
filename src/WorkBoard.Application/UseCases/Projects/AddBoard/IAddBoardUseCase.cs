namespace WorkBoard.Application.UseCases.Projects.AddBoard;

/// <summary>Adds an extra board to an existing project.</summary>
public interface IAddBoardUseCase
{
    /// <summary>Adds the board and returns its id.</summary>
    Task<Guid> ExecuteAsync(Guid projectId, string name, CancellationToken cancellationToken);
}
