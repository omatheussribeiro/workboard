using WorkBoard.Application.DTOs.Dashboard;

namespace WorkBoard.Application.UseCases.Dashboard.GetDashboard;

/// <summary>Builds the workspace dashboard.</summary>
public interface IGetDashboardUseCase
{
    /// <summary>Everything the dashboard renders, in one response.</summary>
    Task<DashboardDto> ExecuteAsync(CancellationToken cancellationToken);
}
