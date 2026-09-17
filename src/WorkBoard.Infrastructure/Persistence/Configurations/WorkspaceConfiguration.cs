using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).IsRequired().HasMaxLength(120);
        builder.Property(w => w.Slug).IsRequired().HasMaxLength(60).IsUnicode(false);
        builder.Property(w => w.Description).HasMaxLength(1000);

        // The slug appears in URLs, so it has to be globally unique, not unique per anything.
        builder.HasIndex(w => w.Slug)
            .IsUnique()
            .HasDatabaseName("UX_Workspaces_Slug");

        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Members)
            .WithOne(m => m.Workspace)
            .HasForeignKey(m => m.WorkspaceId)
            // Deleting a workspace removes its memberships. This is the one cascade path into
            // WorkspaceMembers; the path from Users is Restrict, so SQL Server sees no ambiguity.
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Projects)
            .WithOne(p => p.Workspace)
            .HasForeignKey(p => p.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(w => w.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(w => w.Projects).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(w => w.DomainEvents);
    }
}

internal sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("WorkspaceMembers");

        // Composite key rather than a surrogate id. It enforces "one membership per user per
        // workspace" structurally, and it makes the authorisation lookup — which runs on every
        // authenticated request — a seek on the clustered index with no secondary lookup.
        builder.HasKey(m => new { m.WorkspaceId, m.UserId });

        builder.Property(m => m.Role).HasConversion<int>().IsRequired();

        // The reverse lookup: "which workspaces does this user belong to", used by the workspace
        // switcher and by the tenant middleware. Includes the role so the query never leaves the
        // index to fetch it.
        builder.HasIndex(m => m.UserId)
            .IncludeProperties(m => m.Role)
            .HasDatabaseName("IX_WorkspaceMembers_UserId");
    }
}
