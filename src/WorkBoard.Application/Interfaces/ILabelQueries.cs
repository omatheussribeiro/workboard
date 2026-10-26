using WorkBoard.Application.DTOs.Labels;

namespace WorkBoard.Application.Interfaces;

public interface ILabelQueries
{
    Task<IReadOnlyList<LabelDto>> ListAsync(CancellationToken cancellationToken);
}
