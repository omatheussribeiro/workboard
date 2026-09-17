using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.UseCases.Projects.AddBoard;

/// <inheritdoc cref="IAddBoardUseCase"/>
public sealed class AddBoardUseCase(IProjectRepository projects, IUnitOfWork unitOfWork) : IAddBoardUseCase
{
    /// <inheritdoc/>
    public async Task<Guid> ExecuteAsync(Guid projectId, string name, CancellationToken cancellationToken)
    {
        // With the boards loaded: AddBoard checks that the board belongs to this project and this
        // workspace, and the aggregate keeps the collection consistent.
        var project = await projects.FindWithBoardsAsync(projectId, cancellationToken)
                      ?? throw new NotFoundException("Project", projectId);

        var board = project.AddBoard(Board.CreateDefault(project.WorkspaceId, project.Id, name));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return board.Id;
    }
}
