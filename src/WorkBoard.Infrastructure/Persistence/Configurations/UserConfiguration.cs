using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(256);
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(120);

        // BCrypt output is 60 characters; the column is sized with room for an algorithm change
        // without a migration, but not left as nvarchar(max).
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(200);

        // Unique on the normalised column, not on Email. Comparing a lower-cased column keeps the
        // index seekable and makes uniqueness independent of the database collation, which would
        // otherwise decide whether two addresses differing only in case can both exist.
        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("UX_Users_NormalizedEmail");

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            // Deleting an account should take its sessions with it. Nothing references a refresh
            // token, so there is no cascade path to conflict with.
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Memberships)
            .WithOne(m => m.User)
            .HasForeignKey(m => m.UserId)
            // Restrict, not Cascade: removing a user who owns a workspace must fail loudly rather
            // than quietly delete the workspace with it. Accounts are deactivated, not deleted.
            .OnDelete(DeleteBehavior.Restrict);

        // The backing fields are the source of truth; the public members are read-only projections,
        // so EF must not try to write through them.
        builder.Navigation(u => u.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(u => u.Memberships).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(u => u.DomainEvents);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        // SHA-256 hex, so exactly 64 characters. Fixed-width and non-Unicode: the value is
        // hexadecimal, and nvarchar would double the storage of every row in a table that grows with
        // every login.
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64).IsUnicode(false).IsFixedLength();
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(64).IsUnicode(false);
        builder.Property(t => t.RevokedReason).HasMaxLength(64);

        // Every refresh request looks a token up by hash. Unique because two live tokens hashing to
        // the same value would mean either a collision or a bug, and both should fail at the write.
        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_TokenHash");

        // Covers the "revoke every session for this user" path, filtered to the rows it touches so
        // the index stays small as revoked tokens accumulate.
        builder.HasIndex(t => new { t.UserId, t.ExpiresAtUtc })
            .HasDatabaseName("IX_RefreshTokens_UserId_ExpiresAtUtc")
            .HasFilter("[RevokedAtUtc] IS NULL");

        builder.Ignore(t => t.IsActive);
        builder.Ignore(t => t.DomainEvents);
    }
}
