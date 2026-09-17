using NSubstitute;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.UseCases.Tasks.UpdateTask;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.Tests.Tasks;

/// <summary>Optimistic concurrency on the edit path.</summary>
public sealed class UpdateTaskUseCaseTests
{
    private readonly TaskUseCaseFixture _fixture = new();

    private UpdateTaskUseCase CreateUseCase() => new(
        _fixture.Tasks,
        _fixture.GetTask,
        _fixture.UnitOfWork,
        _fixture.CurrentUser,
        _fixture.Clock);

    /// <summary>
    /// The concurrency token has to reach EF before the save, or the UPDATE carries no WHERE clause
    /// on <c>RowVersion</c> and last write silently wins.
    /// </summary>
    [Fact]
    public async Task Sends_the_clients_row_version_and_commits_with_the_concurrency_check()
    {
        var project = TaskUseCaseFixture.NewProject();
        var task = project.CreateTask(TaskUseCaseFixture.FirstColumn(project).Id, "Original", TaskUseCaseFixture.ActorId);
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        _fixture.Tasks.FindForUpdateAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        await CreateUseCase().ExecuteAsync(
            task.Id,
            new UpdateTaskRequest("Renamed", null, TaskPriority.High, null, rowVersion),
            default);

        _fixture.Tasks.Received(1).SetOriginalRowVersion(task, rowVersion);
        await _fixture.UnitOfWork.Received(1)
            .SaveChangesWithConcurrencyCheckAsync(ConflictMessages.StaleTask, Arg.Any<CancellationToken>());
        task.Title.Should().Be("Renamed");
    }

    [Fact]
    public async Task Rejects_an_unknown_card()
    {
        var act = () => CreateUseCase().ExecuteAsync(
            Guid.CreateVersion7(),
            new UpdateTaskRequest("Renamed", null, TaskPriority.High, null, [1]),
            default);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
