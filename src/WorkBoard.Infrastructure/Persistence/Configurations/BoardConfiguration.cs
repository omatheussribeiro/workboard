using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Infrastructure.Persistence.Configurations;

internal sealed class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("Boards");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(120);

        builder.HasIndex(b => b.ProjectId).HasDatabaseName("IX_Boards_ProjectId");

        builder.HasMany(b => b.Columns)
            .WithOne(c => c.Board)
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Columns).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(b => b.DomainEvents);
    }
}

internal sealed class BoardColumnConfiguration : IEntityTypeConfiguration<BoardColumn>
{
    public void Configure(EntityTypeBuilder<BoardColumn> builder)
    {
        builder.ToTable("BoardColumns");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(60);

        // Stored as int rather than as a string. An enum persisted by name is friendlier to read in
        // the database and four bytes wider per row with a slower comparison; more importantly,
        // renaming a member would then be a silent data migration. The numbers are fixed in the
        // enum for the same reason.
        builder.Property(c => c.Category).HasConversion<int>().IsRequired();

        // Reading a board asks for "the columns of this board, in order". Position is in the key so
        // SQL Server returns them sorted without a separate sort operator.
        builder.HasIndex(c => new { c.BoardId, c.Position })
            .HasDatabaseName("IX_BoardColumns_BoardId_Position");

        builder.HasMany(c => c.Tasks)
            .WithOne(t => t.BoardColumn)
            .HasForeignKey(t => t.BoardColumnId)
            // A column cannot be dropped while it holds cards; the domain refuses it too, and this
            // is the backstop for anything that bypasses the domain.
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(c => c.Tasks).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(c => c.DomainEvents);
    }
}
