using NSubstitute;
using WorkBoard.Application.DTOs.Labels;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Services;
using WorkBoard.Application.UseCases.Labels.CreateLabel;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Tests.Labels;

/// <summary>Label creation, and the uniqueness rule it shares with the rename.</summary>
public sealed class CreateLabelUseCaseTests
{
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();

    private readonly ILabelRepository _labels = Substitute.For<ILabelRepository>();
    private readonly ILabelNameGuard _nameGuard = Substitute.For<ILabelNameGuard>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    private CreateLabelUseCase CreateUseCase()
    {
        _tenant.RequireWorkspaceId().Returns(WorkspaceId);
        return new CreateLabelUseCase(_labels, _nameGuard, _unitOfWork, _tenant);
    }

    [Fact]
    public async Task Rejects_a_name_already_used_in_the_workspace()
    {
        _nameGuard
            .When(guard => guard.EnsureFreeAsync("Bug", null, Arg.Any<CancellationToken>()))
            .Do(_ => throw new ConflictException("taken"));

        var act = () => CreateUseCase().ExecuteAsync(new CreateLabelRequest("Bug", "#ff0000"), default);

        await act.Should().ThrowAsync<ConflictException>();
        _labels.DidNotReceive().Add(Arg.Any<Label>());
    }

    [Fact]
    public async Task Trims_the_name_before_checking_it()
    {
        var created = await CreateUseCase().ExecuteAsync(new CreateLabelRequest("  Bug  ", "#ff0000"), default);

        created.Name.Should().Be("Bug");
        await _nameGuard.Received(1).EnsureFreeAsync("Bug", null, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
