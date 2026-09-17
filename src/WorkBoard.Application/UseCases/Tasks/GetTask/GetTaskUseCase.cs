using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Tasks;

namespace WorkBoard.Application.UseCases.Tasks.GetTask;

/// <inheritdoc cref="IGetTaskUseCase"/>
/// <remarks>
/// Every write that returns a card comes back through here, so the detail payload is produced by one
/// projection rather than by each use case assembling its own.
/// </remarks>
public sealed class GetTaskUseCase(IWorkBoardDbContext db, IClock clock) : IGetTaskUseCase
{
    /// <inheritdoc/>
    public async Task<TaskDetailDto> ExecuteAsync(Guid taskId, CancellationToken cancellationToken) =>
        await db.Tasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(TaskMappings.ToDetail(db, clock.UtcNow))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Task", taskId);
}
