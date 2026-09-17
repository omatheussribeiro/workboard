using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Infrastructure.Persistence.Configurations;

internal sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activities");

        // Non-clustered, because a Guid primary key is randomly ordered as far as SQL Server is
        // concerned (see Entity). Clustering on it would scatter every insert across the table and
        // split pages on the busiest write path in the system.
        builder.HasKey(a => a.Id).IsClustered(false);

        builder.Property(a => a.Type).HasConversion<int>().IsRequired();
        builder.Property(a => a.Summary).IsRequired().HasMaxLength(500);

        // The only nvarchar(max) in the schema, and deliberately so: payloads are small but have no
        // useful upper bound, they are never indexed, and they are never used in a predicate.
        builder.Property(a => a.PayloadJson).HasColumnType("nvarchar(max)");

        // The clustered index, and the shape the feed is read in.
        //
        // Ascending, although the feed reads newest first: an ascending key means each tenant appends
        // to the end of its own range, and SQL Server scans an index backwards just as cheaply as
        // forwards. Clustering descending would make every insert a page split at the head instead.
        //
        // Id is the third column purely as a tiebreaker — two activities can share a millisecond, and
        // without a unique suffix OFFSET/FETCH can return the same row on two pages.
        builder.HasIndex(a => new { a.WorkspaceId, a.OccurredAtUtc, a.Id })
            .IsUnique()
            .IsClustered()
            .HasDatabaseName("CIX_Activities_WorkspaceId_OccurredAtUtc_Id");

        // The per-task history panel. Filtered, because most activities are not task-scoped.
        builder.HasIndex(a => new { a.TaskId, a.OccurredAtUtc })
            .HasDatabaseName("IX_Activities_TaskId_OccurredAtUtc")
            .HasFilter("[TaskId] IS NOT NULL");

        builder.Ignore(a => a.DomainEvents);
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id).IsClustered(false);

        builder.Property(n => n.Type).HasConversion<int>().IsRequired();
        builder.Property(n => n.Message).IsRequired().HasMaxLength(500);

        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        // The unread badge polls this constantly, so it gets a filtered index containing only the
        // rows it asks about. Once a notification is read it drops out of the index entirely, which
        // keeps the index roughly the size of one user's inbox rather than of their whole history.
        builder.HasIndex(n => new { n.RecipientId, n.WorkspaceId })
            .HasDatabaseName("IX_Notifications_RecipientId_WorkspaceId_Unread")
            .HasFilter("[IsRead] = 0");

        // Clustered on the recipient: one user's notifications are always read together, and each
        // user appends to their own contiguous range rather than to a shared hotspot.
        builder.HasIndex(n => new { n.RecipientId, n.CreatedAtUtc, n.Id })
            .IsUnique()
            .IsClustered()
            .HasDatabaseName("CIX_Notifications_RecipientId_CreatedAtUtc_Id");

        builder.Ignore(n => n.DomainEvents);
    }
}
