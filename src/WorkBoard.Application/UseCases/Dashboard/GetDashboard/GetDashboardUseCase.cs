using WorkBoard.Application.DTOs.Dashboard;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Dashboard.GetDashboard;

/// <inheritdoc cref="IGetDashboardUseCase"/>
public sealed class GetDashboardUseCase(IDashboardQueries queries, ICurrentUser currentUser, IClock clock)
    : IGetDashboardUseCase
{
    /// <inheritdoc/>
    public Task<DashboardDto> ExecuteAsync(CancellationToken cancellationToken) =>
        queries.GetAsync(currentUser.RequireUserId(), clock.UtcNow, cancellationToken);
}
