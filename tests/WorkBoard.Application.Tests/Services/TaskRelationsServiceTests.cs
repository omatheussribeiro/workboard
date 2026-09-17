using NSubstitute;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Services;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Tests.Services;

/// <summary>
/// The membership check, which is a security control rather than a convenience.
/// </summary>
/// <remarks>
/// A query filter stops the caller <em>reading</em> other workspaces; it does not constrain what an
/// insert may reference. Without this check a caller could attach an arbitrary user id to a card and
/// see that person's name rendered on it.
/// </remarks>
public sealed class TaskRelationsServiceTests
{
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid ActorId = Guid.CreateVersion7();

    private readonly ILabelRepository _labels = Substitute.For<ILabelRepository>();
    private readonly IWorkspaceRepository _workspaces = Substitute.For<IWorkspaceRepository>();

    private TaskRelationsService CreateService() => new(_labels, _workspaces);

    private static WorkItem NewTask()
    {
        var project = Project.CreateWithDefaultBoard(WorkspaceId, "WorkBoard", "WB", ActorId);
        return project.CreateTask(project.Boards.First().Columns.First().Id, "Card", ActorId);
    }

    [Fact]
    public async Task EnsureMembersAsync_rejects_someone_outside_the_workspace()
    {
        _workspaces.AreAllMembersAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var act = () => CreateService().EnsureMembersAsync([Guid.CreateVersion7()], default);

        (await act.Should().ThrowAsync<ConflictException>()).WithMessage("*not members of this workspace*");
    }

    [Fact]
    public async Task ApplyAssigneesAsync_does_nothing_when_there_are_none()
    {
        await CreateService().ApplyAssigneesAsync(NewTask(), null, ActorId, default);

        await _workspaces.DidNotReceive()
            .AreAllMembersAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>The same member listed twice is one assignment, not a duplicate-key failure.</summary>
    [Fact]
    public async Task ApplyAssigneesAsync_deduplicates()
    {
        var task = NewTask();
        var member = Guid.CreateVersion7();

        _workspaces.AreAllMembersAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await CreateService().ApplyAssigneesAsync(task, [member, member], ActorId, default);

        task.Assignments.Should().ContainSingle();
    }

    /// <summary>
    /// A label id that does not resolve is reported as missing, naming the ids that failed.
    /// </summary>
    /// <remarks>
    /// It does not resolve either because it does not exist or because it belongs to another
    /// workspace — the repository reads through the tenant filter, so the two are the same outcome.
    /// </remarks>
    [Fact]
    public async Task ApplyLabelsAsync_fails_when_a_label_does_not_resolve()
    {
        var present = Label.Create(WorkspaceId, "bug", "#ff0000");
        var missing = Guid.CreateVersion7();

        _labels.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([present]);

        var act = () => CreateService().ApplyLabelsAsync(NewTask(), [present.Id, missing], default);

        (await act.Should().ThrowAsync<NotFoundException>()).WithMessage($"*{missing}*");
    }
}
