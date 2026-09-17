using NSubstitute;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.UseCases.Tasks.ApplyTaskLabel;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Tests.Tasks;

/// <summary>Labels from another workspace are invisible, and therefore not found.</summary>
public sealed class ApplyTaskLabelUseCaseTests
{
    private readonly TaskUseCaseFixture _fixture = new();

    private ApplyTaskLabelUseCase CreateUseCase() => new(_fixture.Tasks, _fixture.Labels, _fixture.UnitOfWork);

    [Fact]
    public async Task Rejects_a_label_that_does_not_exist_in_this_workspace()
    {
        var project = TaskUseCaseFixture.NewProject();
        var task = project.CreateTask(TaskUseCaseFixture.FirstColumn(project).Id, "Card", TaskUseCaseFixture.ActorId);

        _fixture.Tasks.FindForUpdateAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _fixture.Labels.FindForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Label?)null);

        var act = () => CreateUseCase().ExecuteAsync(task.Id, Guid.CreateVersion7(), default);

        await act.Should().ThrowAsync<NotFoundException>();
        await _fixture.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Applies_a_label_that_belongs_here()
    {
        var project = TaskUseCaseFixture.NewProject();
        var task = project.CreateTask(TaskUseCaseFixture.FirstColumn(project).Id, "Card", TaskUseCaseFixture.ActorId);
        var label = Label.Create(TaskUseCaseFixture.WorkspaceId, "bug", "#ff0000");

        _fixture.Tasks.FindForUpdateAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        _fixture.Labels.FindForUpdateAsync(label.Id, Arg.Any<CancellationToken>()).Returns(label);

        await CreateUseCase().ExecuteAsync(task.Id, label.Id, default);

        task.Labels.Should().ContainSingle(applied => applied.LabelId == label.Id);
        await _fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
