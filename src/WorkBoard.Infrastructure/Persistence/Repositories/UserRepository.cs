using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IUserRepository"/>
/// <remarks>
/// <para>
/// Users and refresh tokens are not workspace-scoped — an account exists before it belongs to
/// anything, and authentication happens before a workspace is resolved — so these queries run with
/// the tenant filter suspended. <see cref="WorkBoardDbContext.IgnoringTenantFilter{T}"/> is the one
/// sanctioned way to do that, and confining it to this class is what lets the architecture tests
/// assert nothing else reaches for it.
/// </para>
/// <para>
/// What makes that safe is that no method here takes a workspace: each is keyed by normalised email
/// or by token hash, both of which the caller already holds.
/// </para>
/// </remarks>
public sealed class UserRepository(WorkBoardDbContext context) : IUserRepository
{
    /// <inheritdoc/>
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.IgnoringTenantFilter<User>()
            .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.IgnoringTenantFilter<User>()
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    /// <inheritdoc/>
    public Task<RefreshTokenOwner?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.IgnoringTenantFilter<RefreshToken>()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => new RefreshTokenOwner(token, token.User))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RefreshToken>> ListActiveRefreshTokensAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await context.IgnoringTenantFilter<RefreshToken>()
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public void Add(User user) => context.Users.Add(user);

    /// <inheritdoc/>
    public void AddRefreshToken(RefreshToken token) => context.RefreshTokens.Add(token);
}
