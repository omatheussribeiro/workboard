using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Events;
using WorkBoard.Domain.Exceptions;

namespace WorkBoard.Domain.Tests;

/// <summary>
/// Membership rules, which are the foundation of the authorisation model.
/// </summary>
/// <remarks>
/// These run without a database because the rules do not need one — that is the point of keeping
/// them on the aggregate rather than in a service that queries.
/// </remarks>
public sealed class WorkspaceTests
{
    private static readonly Guid Owner = Guid.CreateVersion7();
    private static readonly Guid Member = Guid.CreateVersion7();

    [Fact]
    public void Creating_a_workspace_makes_the_creator_its_owner()
    {
        var workspace = Workspace.Create("Acme", "acme", Owner);

        workspace.OwnerId.Should().Be(Owner);
        workspace.Members.Should().ContainSingle()
            .Which.Role.Should().Be(WorkspaceRole.Owner);
    }

    [Fact]
    public void Creating_a_workspace_raises_a_domain_event()
    {
        var workspace = Workspace.Create("Acme", "acme", Owner);

        workspace.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkspaceCreated>();
    }

    [Theory]
    [InlineData("Acme")]
    [InlineData("acme_corp")]
    [InlineData("-acme")]
    [InlineData("acme-")]
    [InlineData("acme corp")]
    public void A_slug_that_would_not_survive_a_url_is_rejected(string slug)
    {
        // "Acme" is in the list on purpose: uppercase is normalised, not rejected, so this asserts
        // the normalisation happens rather than that the input fails.
        if (slug == "Acme")
        {
            Workspace.Create("Acme", slug, Owner).Slug.Should().Be("acme");
            return;
        }

        var act = () => Workspace.Create("Acme", slug, Owner);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void A_member_cannot_be_added_twice()
    {
        var workspace = Workspace.Create("Acme", "acme", Owner);
        workspace.AddMember(Member, WorkspaceRole.Member);

        var act = () => workspace.AddMember(Member, WorkspaceRole.Member);

        act.Should().Throw<DomainException>()
            .WithMessage("*already a member*");
    }

    [Fact]
    public void The_owner_cannot_be_removed()
    {
        var workspace = Workspace.Create("Acme", "acme", Owner);

        var act = () => workspace.RemoveMember(Owner);

        act.Should().Throw<DomainException>()
            .WithMessage("*owner cannot be removed*");
    }

    [Fact]
    public void The_owner_cannot_be_demoted_through_a_role_change()
    {
        var workspace = Workspace.Create("Acme", "acme", Owner);

        var act = () => workspace.ChangeMemberRole(Owner, WorkspaceRole.Member);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void A_second_owner_cannot_be_granted()
    {
        var workspace = Workspace.Create("Acme", "acme", Owner);

        var act = () => workspace.AddMember(Member, WorkspaceRole.Owner);

        act.Should().Throw<DomainException>()
            .WithMessage("*exactly one owner*");
    }

    [Fact]
    public void Transferring_ownership_demotes_the_previous_owner_to_admin()
    {
        var workspace = Workspace.Create("Acme", "acme", Owner);
        workspace.AddMember(Member, WorkspaceRole.Member);

        workspace.TransferOwnership(Member);

        workspace.OwnerId.Should().Be(Member);
        workspace.Members.Single(m => m.UserId == Member).Role.Should().Be(WorkspaceRole.Owner);
        workspace.Members.Single(m => m.UserId == Owner).Role.Should().Be(WorkspaceRole.Admin);
    }

    [Fact]
    public void Ownership_cannot_be_transferred_to_someone_outside_the_workspace()
    {
        var workspace = Workspace.Create("Acme", "acme", Owner);

        var act = () => workspace.TransferOwnership(Guid.CreateVersion7());

        act.Should().Throw<DomainException>()
            .WithMessage("*not a member*");
    }

    /// <summary>
    /// The comparison that every authorisation decision reduces to.
    /// </summary>
    [Theory]
    [InlineData(WorkspaceRole.Member, WorkspaceRole.Member, true)]
    [InlineData(WorkspaceRole.Member, WorkspaceRole.Admin, false)]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Member, true)]
    [InlineData(WorkspaceRole.Admin, WorkspaceRole.Admin, true)]
    public void HasAtLeast_compares_roles_by_rank(WorkspaceRole granted, WorkspaceRole required, bool expected)
    {
        var workspace = Workspace.Create("Acme", "acme", Owner);
        workspace.AddMember(Member, granted);

        workspace.HasAtLeast(Member, required).Should().Be(expected);
    }

    [Fact]
    public void A_non_member_has_no_role_at_all()
    {
        var workspace = Workspace.Create("Acme", "acme", Owner);

        workspace.HasAtLeast(Guid.CreateVersion7(), WorkspaceRole.Member).Should().BeFalse();
    }
}
