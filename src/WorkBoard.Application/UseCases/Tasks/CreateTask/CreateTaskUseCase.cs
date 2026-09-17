using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Services;
using WorkBoard.Application.UseCases.Tasks.GetTask;

namespace WorkBoard.Application.UseCases.Tasks.CreateTask;

/// <inheritdoc cref="ICreateTaskUseCase"/>
/// <remarks>
/// The project is loaded tracked because creation mutates it: the per-project card number comes from
/// a counter on the project row, guarded by that row's concurrency token. Two people creating a card
/// at the same moment therefore collide on the token rather than both being handed WB-42, and the
/// loser retries. Card, assignments, labels, the counter bump and the activity rows written by the
/// domain-event dispatcher all land in one commit.
/// </remarks>
public sealed class CreateTaskUseCase(
    ITaskRepository tasks,
    IProjectRepository projects,
    IBoardRepository boards,
    ITaskRelationsService relations,
    IGetTaskUseCase getTask,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : ICreateTaskUseCase
{
    /// <inheritdoc/>
    public async Task<TaskDetailDto> ExecuteAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var actorId = currentUser.RequireUserId();

        var project = await projects.FindForUpdateAsync(request.ProjectId, cancellationToken)
                      ?? throw new NotFoundException("Project", request.ProjectId);

        // Loaded through the tenant-filtered set, so a column id belonging to another workspace
        // simply does not resolve. The project check below catches the in-tenant cross-project case,
        // which no filter can see.
        var placement = await boards.FindColumnPlacementAsync(request.BoardColumnId, cancellationToken)
                        ?? throw new NotFoundException("Board column", request.BoardColumnId);

        if (placement.ProjectId != project.Id)
        {
            throw new ConflictException("The column does not belong to the selected project.");
        }

        var column = placement.Column;

        var task = project.CreateTask(column.Id, request.Title, actorId);
        task.ChangeDescription(request.Description);
        task.ChangePriority(request.Priority, actorId);

        if (request.DueAtUtc is not null)
        {
            task.SetDeadline(request.DueAtUtc, clock.UtcNow, actorId);
        }

        task.PlaceAtEndOf(column, await tasks.GetLastPositionAsync(column.Id, cancellationToken));

        if (request.ParentTaskId is { } parentId)
        {
            var parent = await tasks.FindWithSubtasksAsync(parentId, cancellationToken)
                         ?? throw new NotFoundException("Parent task", parentId);

            parent.AddSubtask(task);
        }

        await relations.ApplyAssigneesAsync(task, request.AssigneeIds, actorId, cancellationToken);
        await relations.ApplyLabelsAsync(task, request.LabelIds, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await getTask.ExecuteAsync(task.Id, cancellationToken);
    }
}
