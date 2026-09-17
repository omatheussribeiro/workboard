using WorkBoard.Application.DTOs.Boards;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Boards.UpdateColumn;

/// <inheritdoc cref="IUpdateColumnUseCase"/>
public sealed class UpdateColumnUseCase(IBoardRepository boards, IUnitOfWork unitOfWork) : IUpdateColumnUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(
        Guid boardId,
        Guid columnId,
        UpdateColumnRequest request,
        CancellationToken cancellationToken)
    {
        var board = await boards.FindWithColumnsAsync(boardId, cancellationToken)
                    ?? throw new NotFoundException("Board", boardId);

        // RequireColumn, not a lookup here: the aggregate refuses a column belonging to another
        // board, which is the check that stops a caller reordering someone else's board by id.
        var column = board.RequireColumn(columnId);

        column.Rename(request.Name);
        column.ChangeCategory(request.Category);
        column.SetWipLimit(request.WipLimit);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
