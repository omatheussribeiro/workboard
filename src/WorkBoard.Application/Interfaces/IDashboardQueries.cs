using WorkBoard.Application.DTOs.Dashboard;

namespace WorkBoard.Application.Interfaces;

public interface IDashboardQueries
{
    Task<DashboardDto> GetAsync(Guid currentUserId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
