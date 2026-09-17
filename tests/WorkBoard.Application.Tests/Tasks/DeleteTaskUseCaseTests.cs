using NSubstitute;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.UseCases.Tasks.DeleteTask;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Tests.Tasks;

/// <summary>Deletion refuses rather than cascading.</summary>
public sealed class DeleteTaskUseCaseTests
{
    private readonly TaskUseCaseFixture _fixture = new();

    private DeleteTaskUseCase CreateUseCase() => new(_fixture.Tasks, _fixture.UnitOfWork);

    [Fact]
    public async Task Refuses_a_task_that_still_has_subtasks()
    {
        var project = TaskUseCaseFixture.NewProject();
        var column = TaskUseCaseFixture.FirstColumn(project);
        var parent = project.CreateTask(column.Id, "Parent", TaskUseCaseFixture.ActorId);
        parent.AddSubtask(project.CreateTask(column.Id, "Child", TaskUseCaseFixture.ActorId));

        _fixture.Tasks.FindWithSubtasksAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);

        var act = () => CreateUseCase().ExecuteAsync(parent.Id, default);

        (await act.Should().ThrowAsync<ConflictException>()).WithMessage("*subtasks*");
        _fixture.Tasks.DidNotReceive().Remove(Arg.Any<WorkItem>());
    }

    [Fact]
    public async Task Deletes_a_task_with_no_subtasks()
    {
        var project = TaskUseCaseFixture.NewProject();
        var task = project.CreateTask(TaskUseCaseFixture.FirstColumn(project).Id, "Standalone", TaskUseCaseFixture.ActorId);

        _fixture.Tasks.FindWithSubtasksAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        await CreateUseCase().ExecuteAsync(task.Id, default);

        _fixture.Tasks.Received(1).Remove(task);
        await _fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
