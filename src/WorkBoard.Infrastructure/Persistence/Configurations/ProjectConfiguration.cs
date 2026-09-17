using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(120);
        builder.Property(p => p.Key).IsRequired().HasMaxLength(10).IsUnicode(false);
        builder.Property(p => p.Description).HasMaxLength(2000);

        // The token that makes per-project task numbering safe. SQL Server maintains rowversion
        // itself, so nothing in the application has to remember to bump it; EF adds it to the WHERE
        // clause of every UPDATE and raises DbUpdateConcurrencyException when no row matches.
        builder.Property(p => p.RowVersion).IsRowVersion();

        // Unique per workspace, not globally: two tenants may both want a project called "WEB".
        // The workspace column leads the index so it doubles as the tenant-filtered lookup.
        builder.HasIndex(p => new { p.WorkspaceId, p.Key })
            .IsUnique()
            .HasDatabaseName("UX_Projects_WorkspaceId_Key");

        // Backs the project list, which is filtered by tenant and almost always excludes archived.
        builder.HasIndex(p => new { p.WorkspaceId, p.IsArchived })
            .IncludeProperties(p => new { p.Name, p.Key })
            .HasDatabaseName("IX_Projects_WorkspaceId_IsArchived");

        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(p => p.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Boards)
            .WithOne(b => b.Project)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            // Restrict, although a project owns its tasks. Tasks are also reachable from
            // BoardColumn, which cascades from Board and therefore from Project; letting both paths
            // cascade is precisely the "multiple cascade paths" SQL Server rejects. Deleting a
            // project is handled in the application, which removes tasks first.
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(p => p.Boards).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Tasks).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.DomainEvents);
    }
}
