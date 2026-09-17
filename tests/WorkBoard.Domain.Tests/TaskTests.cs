using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;
using WorkBoard.Domain.Events;
using WorkBoard.Domain.Exceptions;

namespace WorkBoard.Domain.Tests;

/// <summary>
/// The task rules: numbering, deadlines, board movement and the derived status timestamps.
/// </summary>
public sealed class TaskTests
{
    private static readonly Guid Workspace = Guid.CreateVersion7();
    private static readonly Guid Actor = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private static (Project Project, Board Board) NewProject()
    {
        var project = Project.CreateWithDefaultBoard(Workspace, "Platform", "PLAT", Actor);
        return (project, project.Boards.First());
    }

    private static BoardColumn ColumnOf(Board board, TaskStatusCategory category) =>
        board.Columns.Single(c => c.Category == category);

    [Fact]
    public void Task_numbers_are_sequential_within_the_project()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);

        var first = project.CreateTask(todo.Id, "One", Actor);
        var second = project.CreateTask(todo.Id, "Two", Actor);

        first.Number.Should().Be(1);
        second.Number.Should().Be(2);
        project.FormatTaskKey(second.Number).Should().Be("PLAT-2");
    }

    [Fact]
    public void A_task_inherits_the_workspace_and_project_it_was_created_in()
    {
        var (project, board) = NewProject();

        var task = project.CreateTask(ColumnOf(board, TaskStatusCategory.Todo).Id, "One", Actor);

        task.WorkspaceId.Should().Be(Workspace);
        task.ProjectId.Should().Be(project.Id);
    }

    [Fact]
    public void An_archived_project_accepts_no_new_tasks()
    {
        var (project, board) = NewProject();
        project.Archive();

        var act = () => project.CreateTask(ColumnOf(board, TaskStatusCategory.Todo).Id, "One", Actor);

        act.Should().Throw<DomainException>()
            .WithMessage("*archived*");
    }

    [Fact]
    public void A_deadline_in_the_past_is_rejected()
    {
        var (project, board) = NewProject();
        var task = project.CreateTask(ColumnOf(board, TaskStatusCategory.Todo).Id, "One", Actor);

        var act = () => task.SetDeadline(Now.AddDays(-1), Now, Actor);

        act.Should().Throw<DomainException>()
            .WithMessage("*must be in the future*");
    }

    /// <summary>
    /// The case the rule is carefully written to allow.
    /// </summary>
    /// <remarks>
    /// A deadline that has merely elapsed must stay saveable, or every overdue task in the system
    /// becomes uneditable the moment it goes overdue.
    /// </remarks>
    [Fact]
    public void An_existing_deadline_that_has_elapsed_does_not_block_other_edits()
    {
        var (project, board) = NewProject();
        var task = project.CreateTask(ColumnOf(board, TaskStatusCategory.Todo).Id, "One", Actor);
        task.SetDeadline(Now.AddDays(1), Now, Actor);

        var later = Now.AddDays(5);
        var act = () => task.ChangeTitle("Still editable", Actor);

        act.Should().NotThrow();
        task.IsOverdue(later).Should().BeTrue();
    }

    [Fact]
    public void A_subtask_cannot_be_due_after_its_parent()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);

        var parent = project.CreateTask(todo.Id, "Parent", Actor);
        parent.SetDeadline(Now.AddDays(5), Now, Actor);

        var child = project.CreateTask(todo.Id, "Child", Actor);
        parent.AddSubtask(child);

        var act = () => child.SetDeadline(Now.AddDays(6), Now, Actor);

        act.Should().Throw<DomainException>()
            .WithMessage("*cannot be due after its parent*");
    }

    [Fact]
    public void Subtasks_are_limited_to_one_level()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);

        var grandparent = project.CreateTask(todo.Id, "Grandparent", Actor);
        var parent = project.CreateTask(todo.Id, "Parent", Actor);
        var child = project.CreateTask(todo.Id, "Child", Actor);

        grandparent.AddSubtask(parent);

        var act = () => parent.AddSubtask(child);

        act.Should().Throw<DomainException>()
            .WithMessage("*more than one level*");
    }

    [Fact]
    public void Moving_a_task_to_In_Progress_records_when_it_started()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);
        var inProgress = ColumnOf(board, TaskStatusCategory.InProgress);

        var task = project.CreateTask(todo.Id, "One", Actor);
        task.PlaceAtEndOf(todo, null);

        task.MoveTo(inProgress, todo, null, null, Now, Actor);

        task.StartedAtUtc.Should().Be(Now);
        task.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Moving_a_task_to_Done_records_both_timestamps_even_if_it_never_started()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);
        var done = ColumnOf(board, TaskStatusCategory.Done);

        var task = project.CreateTask(todo.Id, "One", Actor);
        task.PlaceAtEndOf(todo, null);

        task.MoveTo(done, todo, null, null, Now, Actor);

        // Cycle-time reporting needs a start; a card dragged straight to Done would otherwise be
        // dropped from every duration metric.
        task.StartedAtUtc.Should().Be(Now);
        task.CompletedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Moving_a_task_back_to_To_Do_clears_the_status_timestamps()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);
        var done = ColumnOf(board, TaskStatusCategory.Done);

        var task = project.CreateTask(todo.Id, "One", Actor);
        task.PlaceAtEndOf(todo, null);
        task.MoveTo(done, todo, null, null, Now, Actor);

        task.MoveTo(todo, done, null, null, Now.AddHours(1), Actor);

        task.StartedAtUtc.Should().BeNull();
        task.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Moving_a_task_between_columns_raises_a_TaskMoved_event()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);
        var done = ColumnOf(board, TaskStatusCategory.Done);

        var task = project.CreateTask(todo.Id, "One", Actor);
        task.PlaceAtEndOf(todo, null);
        task.ClearDomainEvents();

        task.MoveTo(done, todo, null, null, Now, Actor);

        task.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<TaskMoved>();
    }

    [Fact]
    public void Reordering_within_the_same_column_is_not_a_status_change()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);

        var task = project.CreateTask(todo.Id, "One", Actor);
        task.PlaceAtEndOf(todo, null);
        task.ClearDomainEvents();

        task.MoveTo(todo, todo, 100d, 200d, Now, Actor);

        task.Position.Should().Be(150d);
        task.DomainEvents.Should().BeEmpty("a reorder inside a column is not worth a history entry");
    }

    [Fact]
    public void A_dropped_card_takes_the_midpoint_between_its_neighbours()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);

        var task = project.CreateTask(todo.Id, "One", Actor);
        task.PlaceAtEndOf(todo, null);

        task.MoveTo(todo, todo, 1024d, 2048d, Now, Actor);

        task.Position.Should().Be(1536d);
        task.NeedsRebalance.Should().BeFalse();
    }

    [Fact]
    public void Exhausting_the_gap_between_neighbours_asks_for_a_rebalance()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);

        var task = project.CreateTask(todo.Id, "One", Actor);
        task.PlaceAtEndOf(todo, null);

        task.MoveTo(todo, todo, 1000.0000d, 1000.0001d, Now, Actor);

        task.NeedsRebalance.Should().BeTrue(
            "repeated midpoint insertions eventually run out of representable positions");
    }

    [Fact]
    public void A_column_refuses_a_card_beyond_its_WIP_limit()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);
        var limited = board.AddColumn("Review", TaskStatusCategory.InProgress, wipLimit: 1);

        var first = project.CreateTask(todo.Id, "One", Actor);
        first.PlaceAtEndOf(todo, null);
        first.MoveTo(limited, todo, null, null, Now, Actor);

        var second = project.CreateTask(todo.Id, "Two", Actor);
        second.PlaceAtEndOf(todo, first.Position);

        var act = () => second.MoveTo(limited, todo, null, null, Now, Actor);

        act.Should().Throw<DomainException>()
            .WithMessage("*WIP limit*");
    }

    [Fact]
    public void A_task_cannot_be_moved_into_another_workspace()
    {
        var (project, board) = NewProject();
        var todo = ColumnOf(board, TaskStatusCategory.Todo);

        var task = project.CreateTask(todo.Id, "One", Actor);
        task.PlaceAtEndOf(todo, null);

        var foreignBoard = Board.CreateDefault(Guid.CreateVersion7(), Guid.CreateVersion7(), "Other");
        var foreignColumn = foreignBoard.Columns.First();

        var act = () => task.MoveTo(foreignColumn, todo, null, null, Now, Actor);

        act.Should().Throw<DomainException>()
            .WithMessage("*another workspace*");
    }

    [Fact]
    public void Assigning_the_same_person_twice_is_rejected()
    {
        var (project, board) = NewProject();
        var task = project.CreateTask(ColumnOf(board, TaskStatusCategory.Todo).Id, "One", Actor);
        var assignee = Guid.CreateVersion7();

        task.Assign(assignee, Actor);
        var act = () => task.Assign(assignee, Actor);

        act.Should().Throw<DomainException>()
            .WithMessage("*already assigned*");
    }

    [Fact]
    public void Unassigning_someone_who_was_never_assigned_is_a_no_op()
    {
        var (project, board) = NewProject();
        var task = project.CreateTask(ColumnOf(board, TaskStatusCategory.Todo).Id, "One", Actor);

        var act = () => task.Unassign(Guid.CreateVersion7());

        act.Should().NotThrow("removing something that is not there is the state the caller asked for");
    }

    [Fact]
    public void Changing_a_field_to_the_same_value_records_nothing()
    {
        var (project, board) = NewProject();
        var task = project.CreateTask(ColumnOf(board, TaskStatusCategory.Todo).Id, "One", Actor);
        task.ClearDomainEvents();

        task.ChangeTitle("One", Actor);
        task.ChangePriority(task.Priority, Actor);

        task.DomainEvents.Should().BeEmpty("a history full of no-op edits is a history nobody reads");
    }

    [Fact]
    public void Changing_a_field_records_the_old_and_new_values()
    {
        var (project, board) = NewProject();
        var task = project.CreateTask(ColumnOf(board, TaskStatusCategory.Todo).Id, "One", Actor);
        task.ClearDomainEvents();

        task.ChangePriority(TaskPriority.Critical, Actor);

        var change = task.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TaskFieldChanged>().Subject;

        change.Field.Should().Be(nameof(WorkItem.Priority));
        change.OldValue.Should().Be(nameof(TaskPriority.Medium));
        change.NewValue.Should().Be(nameof(TaskPriority.Critical));
    }
}
