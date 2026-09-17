using NSubstitute;
using WorkBoard.Application.DTOs.Tasks;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.UseCases.Tasks.CreateTask;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.Tests.Tasks;

/// <summary>
/// The rules <see cref="CreateTaskUseCase"/> enforces that the database cannot.
/// </summary>
/// <remarks>
/// These tests exist because of the repositories and the use-case split. The logic used to live in a
/// service that took <c>IWorkBoardDbContext</c> and composed its own queries, so exercising "a column
/// from another project is rejected" meant standing up a provider and seeding four tables.
/// </remarks>
public sealed class CreateTaskUseCaseTests
{
    private readonly TaskUseCaseFixture _fixture = new();

    private CreateTaskUseCase CreateUseCase() => new(
        _fixture.Tasks,
        _fixture.Projects,
        _fixture.Boards,
        _fixture.Relations,
        _fixture.GetTask,
        _fixture.UnitOfWork,
        _fixture.CurrentUser,
        _fixture.Clock);

    private static CreateTaskRequest Request(Guid projectId, Guid columnId) => new(
        projectId,
        columnId,
        "Write the ADR",
        null,
        TaskPriority.Medium,
        null,
        null,
        null,
        null);

    [Fact]
    public async Task Rejects_an_unknown_project()
    {
        var act = () => CreateUseCase().ExecuteAsync(Request(Guid.CreateVersion7(), Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<NotFoundException>();
        await _fixture.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The in-tenant cross-project case, which the query filter cannot catch.
    /// </summary>
    /// <remarks>
    /// Both the project and the column belong to this workspace, so every filter is satisfied; only
    /// the explicit check stops a card landing on a board belonging to a different project.
    /// </remarks>
    [Fact]
    public async Task Rejects_a_column_belonging_to_another_project()
    {
        var project = TaskUseCaseFixture.NewProject();
        var otherProject = TaskUseCaseFixture.NewProject("OT");
        var foreignColumn = TaskUseCaseFixture.FirstColumn(otherProject);

        _fixture.Projects.FindForUpdateAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _fixture.Boards.FindColumnPlacementAsync(foreignColumn.Id, Arg.Any<CancellationToken>())
            .Returns(new ColumnPlacement(foreignColumn, otherProject.Id));

        var act = () => CreateUseCase().ExecuteAsync(Request(project.Id, foreignColumn.Id), default);

        (await act.Should().ThrowAsync<ConflictException>())
            .WithMessage("*does not belong to the selected project*");
        await _fixture.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// One commit, not one per relation.
    /// </summary>
    /// <remarks>
    /// The card, the project's task counter and the assignments have to land together, or the project
    /// hands the next caller a number that is already taken. Asserting the call count is how that
    /// stays true when someone adds a fourth thing to the method.
    /// </remarks>
    [Fact]
    public async Task Commits_once_and_returns_the_card_through_the_read_use_case()
    {
        var project = TaskUseCaseFixture.NewProject();
        var column = TaskUseCaseFixture.FirstColumn(project);

        _fixture.Projects.FindForUpdateAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _fixture.Boards.FindColumnPlacementAsync(column.Id, Arg.Any<CancellationToken>())
            .Returns(new ColumnPlacement(column, project.Id));
        _fixture.Tasks.GetLastPositionAsync(column.Id, Arg.Any<CancellationToken>()).Returns((double?)null);

        await CreateUseCase().ExecuteAsync(Request(project.Id, column.Id), default);

        await _fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _fixture.GetTask.Received(1).ExecuteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Assignees and labels are delegated, not re-implemented.</summary>
    [Fact]
    public async Task Applies_assignees_and_labels_through_the_shared_service()
    {
        var project = TaskUseCaseFixture.NewProject();
        var column = TaskUseCaseFixture.FirstColumn(project);
        var assignee = Guid.CreateVersion7();
        var label = Guid.CreateVersion7();

        _fixture.Projects.FindForUpdateAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _fixture.Boards.FindColumnPlacementAsync(column.Id, Arg.Any<CancellationToken>())
            .Returns(new ColumnPlacement(column, project.Id));

        var request = Request(project.Id, column.Id) with
        {
            AssigneeIds = [assignee],
            LabelIds = [label],
        };

        await CreateUseCase().ExecuteAsync(request, default);

        await _fixture.Relations.Received(1).ApplyAssigneesAsync(
            Arg.Any<Domain.Entities.WorkItem>(),
            Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(assignee)),
            TaskUseCaseFixture.ActorId,
            Arg.Any<CancellationToken>());

        await _fixture.Relations.Received(1).ApplyLabelsAsync(
            Arg.Any<Domain.Entities.WorkItem>(),
            Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(label)),
            Arg.Any<CancellationToken>());
    }
}
