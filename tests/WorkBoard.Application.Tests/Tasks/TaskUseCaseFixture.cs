using NSubstitute;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Services;
using WorkBoard.Application.UseCases.Tasks.GetTask;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Tests.Tasks;

/// <summary>
/// The substitutes every task use case needs, arranged once.
/// </summary>
/// <remarks>
/// Each use case takes a narrow set of these, which is itself the observation worth making: before
/// the split there was one class with nine dependencies, and a test for "delete refuses a card with
/// subtasks" had to construct all nine.
/// </remarks>
internal sealed class TaskUseCaseFixture
{
    public static readonly Guid WorkspaceId = Guid.CreateVersion7();
    public static readonly Guid ActorId = Guid.CreateVersion7();

    public ITaskRepository Tasks { get; } = Substitute.For<ITaskRepository>();

    public IProjectRepository Projects { get; } = Substitute.For<IProjectRepository>();

    public IBoardRepository Boards { get; } = Substitute.For<IBoardRepository>();

    public ILabelRepository Labels { get; } = Substitute.For<ILabelRepository>();

    public ITaskRelationsService Relations { get; } = Substitute.For<ITaskRelationsService>();

    public IGetTaskUseCase GetTask { get; } = Substitute.For<IGetTaskUseCase>();

    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new(ActorId);

    /// <summary>A project with the default three-column board, built through the domain factory.</summary>
    public static Project NewProject(string key = "WB") =>
        Project.CreateWithDefaultBoard(WorkspaceId, "WorkBoard", key, ActorId);

    /// <summary>The leftmost column of a project's default board.</summary>
    public static BoardColumn FirstColumn(Project project) =>
        project.Boards.First().Columns.OrderBy(column => column.Position).First();
}
