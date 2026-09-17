using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Exceptions;

namespace WorkBoard.Infrastructure.Persistence.Context;

/// <inheritdoc cref="IUnitOfWork"/>
/// <remarks>
/// A thin wrapper over the <see cref="WorkBoardDbContext"/> the repositories share, which is what
/// makes "one unit of work" mean something concrete: they all stage into the same change tracker, so
/// a service that touches four aggregates still commits once, in one transaction.
/// </remarks>
public sealed class UnitOfWork(WorkBoardDbContext context) : IUnitOfWork
{
    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task SaveChangesWithConcurrencyCheckAsync(
        string conflictMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Translated here rather than in the global exception handler, so the message can
            // describe what the user actually did, and so the application layer owns its own failure
            // vocabulary instead of importing EF Core's into the transport.
            throw new ConflictException(conflictMessage);
        }
    }

    /// <inheritdoc/>
    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(
            cancellationToken,
            async (token) =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(token);
                await operation(token);
                await transaction.CommitAsync(token);
            });
}
