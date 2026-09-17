using WorkBoard.Application.DTOs.Tasks;

namespace WorkBoard.Application.UseCases.Tasks.CreateTask;

/// <summary>Opens a new card at the end of its column.</summary>
public interface ICreateTaskUseCase
{
    /// <summary>Creates the card and returns it in full.</summary>
    Task<TaskDetailDto> ExecuteAsync(CreateTaskRequest request, CancellationToken cancellationToken);
}
