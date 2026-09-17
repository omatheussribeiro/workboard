using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Infrastructure.Persistence.Configurations;

internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Body).IsRequired().HasMaxLength(5000);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // A comment thread is read in creation order for one task. Filtered on the soft-delete flag
        // so tombstones, which are never read, do not sit in the index.
        builder.HasIndex(c => new { c.TaskId, c.CreatedAtUtc })
            .HasDatabaseName("IX_Comments_TaskId_CreatedAtUtc")
            .HasFilter("[IsDeleted] = 0");

        builder.Ignore(c => c.DomainEvents);
    }
}

internal sealed class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("Labels");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).IsRequired().HasMaxLength(40);

        // Six hex characters, stored without the leading hash and without Unicode.
        builder.Property(l => l.ColorHex).IsRequired().HasMaxLength(6).IsUnicode(false).IsFixedLength();

        // Label names are unique within a workspace. This is the constraint that makes the race
        // between two concurrent "create label" requests safe, rather than the application check
        // that precedes it for the sake of a readable error.
        builder.HasIndex(l => new { l.WorkspaceId, l.Name })
            .IsUnique()
            .HasDatabaseName("UX_Labels_WorkspaceId_Name");

        builder.HasOne(l => l.Workspace)
            .WithMany()
            .HasForeignKey(l => l.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Tasks).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(l => l.DomainEvents);
    }
}
