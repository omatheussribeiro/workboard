using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Tasks.GetTask;

/// <inheritdoc cref="IGetTaskUseCase"/>
/// <remarks>
/// Every write that returns a card comes back through here, so the detail payload is produced by one
/// projection rather than by each use case assembling its own.
/// </remarks>
public sealed class GetTaskUseCase(ITaskQueries queries, IClock clock) : IGetTaskUseCase
{
    /// <inheritdoc/>
    public async Task<TaskDetailDto> ExecuteAsync(Guid taskId, CancellationToken cancellationToken) =>
        await queries.GetAsync(taskId, clock.UtcNow, cancellationToken)
        ?? throw new NotFoundException("Task", taskId);
}
