using WorkBoard.Application.DTOs.Tasks;

namespace WorkBoard.Application.UseCases.Tasks.UpdateTask;

/// <summary>Edits a card, subject to the concurrency token in the request.</summary>
public interface IUpdateTaskUseCase
{
    /// <summary>Applies the edit and returns the card in full.</summary>
    Task<TaskDetailDto> ExecuteAsync(Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken);
}
