using WorkBoard.Application.DTOs.Tasks;

namespace WorkBoard.Application.UseCases.Tasks.AssignTask;

/// <summary>Assigns a workspace member to a card.</summary>
public interface IAssignTaskUseCase
{
    /// <summary>Assigns them, or reports a conflict when they are not a member.</summary>
    Task ExecuteAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken);
}
