using WorkBoard.Application.DTOs.Tasks;

namespace WorkBoard.Application.UseCases.Tasks.MoveTask;

/// <summary>Applies a drag-and-drop move, subject to the concurrency token in the request.</summary>
public interface IMoveTaskUseCase
{
    /// <summary>Moves the card and returns it in full.</summary>
    Task<TaskDetailDto> ExecuteAsync(Guid taskId, MoveTaskRequest request, CancellationToken cancellationToken);
}
