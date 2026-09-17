using NSubstitute;
using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.UseCases.Workspaces.AddWorkspaceMember;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.Tests.Workspaces;

/// <summary>Membership is granted only to accounts that already exist.</summary>
public sealed class AddWorkspaceMemberUseCaseTests
{
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly IWorkspaceRepository _workspaces = Substitute.For<IWorkspaceRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestClock _clock = new();

    private AddWorkspaceMemberUseCase CreateUseCase() =>
        new(_workspaces, _users, _unitOfWork, _clock);

    /// <remarks>
    /// Saying "that account is disabled" would confirm the address is registered to anyone who can
    /// add members, which is every admin of every workspace.
    /// </remarks>
    [Fact]
    public async Task Reports_a_deactivated_account_as_not_found()
    {
        var workspace = Workspace.Create("Acme", "acme", OwnerId);
        var user = User.Register("gone@workboard.test", "Gone", "hash");
        user.Deactivate();

        _workspaces.FindForUpdateAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _users.FindByEmailAsync("gone@workboard.test", Arg.Any<CancellationToken>()).Returns(user);

        var act = () => CreateUseCase().ExecuteAsync(
            workspace.Id,
            new AddMemberRequest("gone@workboard.test", WorkspaceRole.Member),
            default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Reports_an_unknown_address_as_not_found()
    {
        var workspace = Workspace.Create("Acme", "acme", OwnerId);

        _workspaces.FindForUpdateAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _users.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => CreateUseCase().ExecuteAsync(
            workspace.Id,
            new AddMemberRequest("nobody@workboard.test", WorkspaceRole.Member),
            default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Adds_an_active_account()
    {
        var workspace = Workspace.Create("Acme", "acme", OwnerId);
        var user = User.Register("new@workboard.test", "New Person", "hash");

        _workspaces.FindForUpdateAsync(workspace.Id, Arg.Any<CancellationToken>()).Returns(workspace);
        _users.FindByEmailAsync("new@workboard.test", Arg.Any<CancellationToken>()).Returns(user);

        var member = await CreateUseCase().ExecuteAsync(
            workspace.Id,
            new AddMemberRequest("New@WorkBoard.test", WorkspaceRole.Member),
            default);

        member.UserId.Should().Be(user.Id);
        member.Role.Should().Be(WorkspaceRole.Member);
        workspace.Members.Should().Contain(membership => membership.UserId == user.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
