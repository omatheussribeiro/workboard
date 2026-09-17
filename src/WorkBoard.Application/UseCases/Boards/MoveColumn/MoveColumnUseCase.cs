using WorkBoard.Application.DTOs.Boards;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Boards.MoveColumn;

/// <inheritdoc cref="IMoveColumnUseCase"/>
public sealed class MoveColumnUseCase(IBoardRepository boards, IUnitOfWork unitOfWork) : IMoveColumnUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(
        Guid boardId,
        Guid columnId,
        MoveColumnRequest request,
        CancellationToken cancellationToken)
    {
        var board = await boards.FindWithColumnsAsync(boardId, cancellationToken)
                    ?? throw new NotFoundException("Board", boardId);

        board.MoveColumn(columnId, request.Position);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
