using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Services;

namespace WorkBoard.Application.UseCases.Tasks.AssignTask;

/// <inheritdoc cref="IAssignTaskUseCase"/>
public sealed class AssignTaskUseCase(
    ITaskRepository tasks,
    ITaskRelationsService relations,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IAssignTaskUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken)
    {
        var actorId = currentUser.RequireUserId();

        var task = await tasks.FindForUpdateAsync(taskId, cancellationToken)
                   ?? throw new NotFoundException("Task", taskId);

        await relations.EnsureMembersAsync([request.UserId], cancellationToken);

        task.Assign(request.UserId, actorId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
