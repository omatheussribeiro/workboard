using WorkBoard.Application.DTOs.Tasks;

namespace WorkBoard.Application.UseCases.Tasks.GetTask;

/// <summary>Reads one card in full.</summary>
public interface IGetTaskUseCase
{
    /// <summary>Returns the card.</summary>
    /// <exception cref="WorkBoard.Application.Exceptions.NotFoundException">The card does not exist in this workspace.</exception>
    Task<TaskDetailDto> ExecuteAsync(Guid taskId, CancellationToken cancellationToken);
}
