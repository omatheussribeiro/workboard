using NSubstitute;
using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.UseCases.Workspaces.CreateWorkspace;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.Tests.Workspaces;

/// <summary>Creating a workspace creates a tenant, so the slug has to be globally unique.</summary>
public sealed class CreateWorkspaceUseCaseTests
{
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly IWorkspaceRepository _workspaces = Substitute.For<IWorkspaceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateWorkspaceUseCase CreateUseCase() =>
        new(_workspaces, _unitOfWork, new TestCurrentUser(OwnerId, WorkspaceRole.Admin));

    [Fact]
    public async Task Rejects_a_slug_that_is_already_taken()
    {
        _workspaces.SlugExistsAsync("acme", Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateUseCase().ExecuteAsync(new CreateWorkspaceRequest("Acme", "ACME", null), default);

        (await act.Should().ThrowAsync<ConflictException>()).WithMessage("*acme*");
        _workspaces.DidNotReceive().Add(Arg.Any<Workspace>());
    }

    [Fact]
    public async Task Makes_the_caller_the_owner()
    {
        var created = await CreateUseCase().ExecuteAsync(new CreateWorkspaceRequest("Acme", "acme", null), default);

        created.MyRole.Should().Be(WorkspaceRole.Owner);
        created.MemberCount.Should().Be(1);
        _workspaces.Received(1).Add(Arg.Is<Workspace>(workspace => workspace.OwnerId == OwnerId));
    }

    /// <summary>The slug is lower-cased before both the check and the insert, so they cannot disagree.</summary>
    [Fact]
    public async Task Normalises_the_slug()
    {
        var created = await CreateUseCase().ExecuteAsync(new CreateWorkspaceRequest("Acme", "  ACME  ", null), default);

        created.Slug.Should().Be("acme");
        await _workspaces.Received(1).SlugExistsAsync("acme", Arg.Any<CancellationToken>());
    }
}
