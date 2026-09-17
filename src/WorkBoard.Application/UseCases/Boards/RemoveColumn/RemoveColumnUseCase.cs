using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Boards.RemoveColumn;

/// <inheritdoc cref="IRemoveColumnUseCase"/>
public sealed class RemoveColumnUseCase(IBoardRepository boards, IUnitOfWork unitOfWork) : IRemoveColumnUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid boardId, Guid columnId, CancellationToken cancellationToken)
    {
        var board = await boards.FindWithColumnsAsync(boardId, cancellationToken)
                    ?? throw new NotFoundException("Board", boardId);

        board.RemoveColumn(columnId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
