using WorkBoard.Application.DTOs.Boards;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Boards.AddColumn;

/// <inheritdoc cref="IAddColumnUseCase"/>
/// <remarks>
/// The board is loaded with its columns, because the aggregate enforces rules across the whole set —
/// unique names and dense positions — and would otherwise compute the new position from an empty
/// collection. That is a handful of rows, which is why the <c>Include</c> is right here and wrong on
/// the read path.
/// </remarks>
public sealed class AddColumnUseCase(IBoardRepository boards, IUnitOfWork unitOfWork) : IAddColumnUseCase
{
    /// <inheritdoc/>
    public async Task<Guid> ExecuteAsync(
        Guid boardId,
        CreateColumnRequest request,
        CancellationToken cancellationToken)
    {
        var board = await boards.FindWithColumnsAsync(boardId, cancellationToken)
                    ?? throw new NotFoundException("Board", boardId);

        var column = board.AddColumn(request.Name, request.Category, request.WipLimit);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return column.Id;
    }
}
