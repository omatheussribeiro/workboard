using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Exceptions;

namespace WorkBoard.Domain.Tests;

public sealed class CommentTests
{
    private static readonly Guid Workspace = Guid.CreateVersion7();
    private static readonly Guid Author = Guid.CreateVersion7();
    private static readonly Guid Someone = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static Comment NewComment()
    {
        var project = Project.CreateWithDefaultBoard(Workspace, "Platform", "PLAT", Author);
        var column = project.Boards.First().Columns.First(c => c.Category == TaskStatusCategory.Todo);
        var task = project.CreateTask(column.Id, "One", Author);

        return task.AddComment("First!", Author, Now);
    }

    [Fact]
    public void Only_the_author_can_edit_a_comment()
    {
        var comment = NewComment();

        var act = () => comment.Edit("Changed", Someone, Now);

        act.Should().Throw<DomainException>()
            .WithMessage("*Only the author*");
    }

    [Fact]
    public void Editing_records_when_it_happened()
    {
        var comment = NewComment();

        comment.Edit("Changed", Author, Now.AddMinutes(5));

        comment.Body.Should().Be("Changed");
        comment.EditedAtUtc.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void A_workspace_admin_can_delete_someone_elses_comment()
    {
        var comment = NewComment();

        comment.Delete(Someone, actorIsWorkspaceAdmin: true);

        comment.IsDeleted.Should().BeTrue();
        comment.Body.Should().BeEmpty("a deleted comment keeps its place in the thread but not its text");
    }

    [Fact]
    public void An_ordinary_member_cannot_delete_someone_elses_comment()
    {
        var comment = NewComment();

        var act = () => comment.Delete(Someone, actorIsWorkspaceAdmin: false);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void A_deleted_comment_cannot_be_edited_back_into_existence()
    {
        var comment = NewComment();
        comment.Delete(Author, actorIsWorkspaceAdmin: false);

        var act = () => comment.Edit("Undelete me", Author, Now);

        act.Should().Throw<DomainException>();
    }
}

public sealed class LabelTests
{
    private static readonly Guid Workspace = Guid.CreateVersion7();

    [Theory]
    [InlineData("#EF4444", "EF4444")]
    [InlineData("ef4444", "EF4444")]
    [InlineData("#ef4444", "EF4444")]
    public void Colours_are_normalised_to_bare_uppercase_hex(string input, string expected)
    {
        Label.Create(Workspace, "bug", input).ColorHex.Should().Be(expected);
    }

    [Theory]
    [InlineData("EF444")]
    [InlineData("EF44444")]
    [InlineData("GGGGGG")]
    [InlineData("")]
    public void A_value_that_is_not_a_six_digit_hex_colour_is_rejected(string input)
    {
        var act = () => Label.Create(Workspace, "bug", input);

        act.Should().Throw<DomainException>();
    }
}

public sealed class BoardTests
{
    private static readonly Guid Workspace = Guid.CreateVersion7();
    private static readonly Guid Project = Guid.CreateVersion7();

    [Fact]
    public void A_new_board_starts_with_the_three_columns_every_team_expects()
    {
        var board = Board.CreateDefault(Workspace, Project, "Board");

        board.Columns.OrderBy(c => c.Position).Select(c => c.Name)
            .Should().Equal("To Do", "In Progress", "Done");
    }

    [Fact]
    public void Column_names_are_unique_within_a_board()
    {
        var board = Board.CreateDefault(Workspace, Project, "Board");

        var act = () => board.AddColumn("to do", TaskStatusCategory.Todo);

        act.Should().Throw<DomainException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void Moving_a_column_renumbers_the_rest_densely()
    {
        var board = Board.CreateDefault(Workspace, Project, "Board");
        var done = board.Columns.Single(c => c.Name == "Done");

        board.MoveColumn(done.Id, 0);

        board.Columns.OrderBy(c => c.Position).Select(c => c.Name)
            .Should().Equal("Done", "To Do", "In Progress");
        board.Columns.Select(c => c.Position).OrderBy(p => p)
            .Should().Equal([0, 1, 2], "positions stay a dense 0..n-1 sequence");
    }

    [Fact]
    public void A_board_must_keep_at_least_one_column()
    {
        var board = Board.CreateDefault(Workspace, Project, "Board");

        foreach (var column in board.Columns.OrderBy(c => c.Position).Take(2).ToList())
        {
            board.RemoveColumn(column.Id);
        }

        var last = board.Columns.Single();
        var act = () => board.RemoveColumn(last.Id);

        act.Should().Throw<DomainException>()
            .WithMessage("*at least one column*");
    }

    [Fact]
    public void A_column_holding_cards_cannot_be_removed()
    {
        var project = WorkBoard.Domain.Entities.Project.CreateWithDefaultBoard(Workspace, "Platform", "PLAT", Guid.CreateVersion7());
        var board = project.Boards.First();
        var todo = board.Columns.Single(c => c.Category == TaskStatusCategory.Todo);

        var task = project.CreateTask(todo.Id, "One", Guid.CreateVersion7());
        task.PlaceAtEndOf(todo, null);

        var act = () => board.RemoveColumn(todo.Id);

        act.Should().Throw<DomainException>()
            .WithMessage("*Move or delete the tasks*");
    }

    [Fact]
    public void A_zero_or_negative_WIP_limit_is_rejected()
    {
        var board = Board.CreateDefault(Workspace, Project, "Board");

        var act = () => board.AddColumn("Review", TaskStatusCategory.InProgress, wipLimit: 0);

        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }
}
