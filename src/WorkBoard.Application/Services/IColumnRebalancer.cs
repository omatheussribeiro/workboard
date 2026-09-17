namespace WorkBoard.Application.Services;

/// <summary>Rewrites the positions of one column as an evenly spaced sequence.</summary>
public interface IColumnRebalancer
{
    /// <summary>Renumbers the column, then commits.</summary>
    Task RebalanceAsync(Guid columnId, CancellationToken cancellationToken);
}
