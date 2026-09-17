using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Infrastructure.Persistence.Configurations;

internal sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        // The one place the CLR name WorkItem is mapped back to the domain name. Everything the
        // database, the API and the UI show says "task"; only C# sees WorkItem, and only because
        // Task is taken. See the type's own remarks and ADR-007.
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id).IsClustered(false);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(10_000);
        builder.Property(t => t.Priority).HasConversion<int>().IsRequired();
        builder.Property(t => t.Position).IsRequired();
        builder.Property(t => t.RowVersion).IsRowVersion();

        // NeedsRebalance is a signal the domain computes for the caller of a single move; persisting
        // it would imply it survives the request, which it does not.
        builder.Ignore(t => t.NeedsRebalance);
        builder.Ignore(t => t.IsSubtask);
        builder.Ignore(t => t.DomainEvents);

        // This is both the uniqueness rule and the clustered index, which is a happy coincidence
        // worth taking.
        //
        // As a constraint: the human-readable key (WB-42) must be unique inside its project. Enforced
        // here rather than trusted to the in-memory counter alone, so a lost concurrency race
        // surfaces as a constraint violation instead of two tasks quietly sharing a number.
        //
        // As the clustered key: (ProjectId, Number) is strictly ascending within each project, so
        // rows physically cluster by project and each insert appends to that project's range.
        // The alternative — clustering on the Guid primary key — would place every new task at a
        // random page (see Entity on SQL Server GUID ordering).
        builder.HasIndex(t => new { t.ProjectId, t.Number })
            .IsUnique()
            .IsClustered()
            .HasDatabaseName("CUX_Tasks_ProjectId_Number");

        // The board read: every card in a column, in order. Position follows the column so the
        // ordering comes free, and the included columns make it a covering index for the card
        // projection — SQL Server answers without touching the clustered index, which is what keeps
        // the 10 KB Description column out of the picture.
        builder.HasIndex(t => new { t.BoardColumnId, t.Position })
            .IncludeProperties(t => new { t.Title, t.Priority, t.DueAtUtc, t.ParentTaskId })
            .HasDatabaseName("IX_Tasks_BoardColumnId_Position");

        // The overdue and deadline queries on the dashboard. Filtered, because a task with no
        // deadline can never match and there is no reason to index it — on a mature workspace that
        // typically excludes most rows.
        builder.HasIndex(t => new { t.WorkspaceId, t.DueAtUtc })
            .HasDatabaseName("IX_Tasks_WorkspaceId_DueAtUtc")
            .HasFilter("[DueAtUtc] IS NOT NULL AND [CompletedAtUtc] IS NULL");

        // Backs the default task list ordering, already narrowed by tenant.
        builder.HasIndex(t => new { t.WorkspaceId, t.CreatedAtUtc })
            .HasDatabaseName("IX_Tasks_WorkspaceId_CreatedAtUtc");

        builder.HasIndex(t => t.ParentTaskId)
            .HasDatabaseName("IX_Tasks_ParentTaskId")
            .HasFilter("[ParentTaskId] IS NOT NULL");

        builder.HasOne(t => t.ParentTask)
            .WithMany(t => t.Subtasks)
            .HasForeignKey(t => t.ParentTaskId)
            // A self-reference can never cascade in SQL Server. Detaching subtasks before deleting a
            // parent is the application's job, and TaskService.DeleteAsync refuses otherwise.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Assignments)
            .WithOne(a => a.Task)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Labels)
            .WithOne(l => l.Task)
            .HasForeignKey(l => l.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Comments)
            .WithOne(c => c.Task)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Assignments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.Labels).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.Comments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.Subtasks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class TaskAssignmentConfiguration : IEntityTypeConfiguration<TaskAssignment>
{
    public void Configure(EntityTypeBuilder<TaskAssignment> builder)
    {
        builder.ToTable("TaskAssignments");

        builder.HasKey(a => new { a.TaskId, a.UserId });

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            // The cascade into this table already comes from Tasks; a second one from Users would be
            // the multiple-cascade-path error again.
            .OnDelete(DeleteBehavior.Restrict);

        // "Tasks assigned to me" — the dashboard and the assignee filter. The task id is the second
        // key column, so the reverse lookup needs its own index.
        builder.HasIndex(a => new { a.UserId, a.WorkspaceId })
            .HasDatabaseName("IX_TaskAssignments_UserId_WorkspaceId");
    }
}

internal sealed class TaskLabelConfiguration : IEntityTypeConfiguration<TaskLabel>
{
    public void Configure(EntityTypeBuilder<TaskLabel> builder)
    {
        builder.ToTable("TaskLabels");

        builder.HasKey(l => new { l.TaskId, l.LabelId });

        builder.HasOne(l => l.Label)
            .WithMany(l => l.Tasks)
            .HasForeignKey(l => l.LabelId)
            // Deleting a label untags its tasks. This is the only cascade into TaskLabels from
            // Labels; the one from Tasks is on the other key column and does not conflict.
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasIndex(l => l.LabelId).HasDatabaseName("IX_TaskLabels_LabelId");

        builder.Navigation(l => l.Label).UsePropertyAccessMode(PropertyAccessMode.Property);
    }
}
