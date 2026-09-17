using NSubstitute;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.UseCases.Workspaces.ArchiveWorkspace;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.Tests.Workspaces;

/// <summary>Archiving is owner-only, even though every other administrative action is admin-enough.</summary>
public sealed class ArchiveWorkspaceUseCaseTests
{
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly IWorkspaceRepository _workspaces = Substitute.For<IWorkspaceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ArchiveWorkspaceUseCase CreateUseCase(Guid actingAs) =>
        new(_workspaces, _unitOfWork, new TestCurrentUser(actingAs, WorkspaceRole.Admin));

    /// <remarks>
    /// It takes the whole tenant offline for every member, so it is reserved to the one account that
    /// cannot itself be removed. The check is in the use case rather than in a policy because the
    /// policy has no way to express "the owner" — it only knows roles.
    /// </remarks>
    [Fact]
    public async Task Refuses_anyone_but_the_owner()
    {
        var workspace = Workspace.Create("Acme", "acme", OwnerId);
        _workspaces.FindForUpdateAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);

        var act = () => CreateUseCase(Guid.CreateVersion7()).ExecuteAsync(workspace.Id, default);

        (await act.Should().ThrowAsync<ForbiddenException>()).WithMessage("*owner*");
        workspace.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task Lets_the_owner_through()
    {
        var workspace = Workspace.Create("Acme", "acme", OwnerId);
        _workspaces.FindForUpdateAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);

        await CreateUseCase(OwnerId).ExecuteAsync(workspace.Id, default);

        workspace.IsArchived.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
