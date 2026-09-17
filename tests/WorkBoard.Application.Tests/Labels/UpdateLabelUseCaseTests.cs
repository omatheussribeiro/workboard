using NSubstitute;
using WorkBoard.Application.DTOs.Labels;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Services;
using WorkBoard.Application.UseCases.Labels.UpdateLabel;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Tests.Labels;

/// <summary>Renaming is checked against the same constraint as creating.</summary>
public sealed class UpdateLabelUseCaseTests
{
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();

    private readonly ILabelRepository _labels = Substitute.For<ILabelRepository>();
    private readonly ILabelNameGuard _nameGuard = Substitute.For<ILabelNameGuard>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdateLabelUseCase CreateUseCase() => new(_labels, _nameGuard, _unitOfWork);

    /// <summary>
    /// Renaming onto an existing name is the same collision as creating one.
    /// </summary>
    /// <remarks>
    /// It was not checked before this refactor, so the rename reached the unique index and surfaced
    /// as a 500 where the identical conflict at creation returned a 409.
    /// </remarks>
    [Fact]
    public async Task Rejects_a_rename_onto_an_existing_name()
    {
        var label = Label.Create(WorkspaceId, "Chore", "#00ff00");

        _labels.FindForUpdateAsync(label.Id, Arg.Any<CancellationToken>()).Returns(label);
        _nameGuard
            .When(guard => guard.EnsureFreeAsync("Bug", label.Id, Arg.Any<CancellationToken>()))
            .Do(_ => throw new ConflictException("taken"));

        var act = () => CreateUseCase().ExecuteAsync(label.Id, new UpdateLabelRequest("Bug", "#ff0000"), default);

        await act.Should().ThrowAsync<ConflictException>();
        label.Name.Should().Be("Chore", "the rename must not be applied before the check");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>The label being renamed is excluded from its own uniqueness check.</summary>
    [Fact]
    public async Task Allows_recolouring_without_changing_the_name()
    {
        var label = Label.Create(WorkspaceId, "Bug", "#ff0000");

        _labels.FindForUpdateAsync(label.Id, Arg.Any<CancellationToken>()).Returns(label);

        await CreateUseCase().ExecuteAsync(label.Id, new UpdateLabelRequest("Bug", "#0000ff"), default);

        label.ColorHex.Should().Be("0000FF");
        await _nameGuard.Received(1).EnsureFreeAsync("Bug", label.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
