using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Labels;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Labels;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

public sealed class LabelQueries(WorkBoardDbContext context) : ILabelQueries
{
    public async Task<IReadOnlyList<LabelDto>> ListAsync(CancellationToken cancellationToken) =>
        await context.Labels
            .AsNoTracking()
            .OrderBy(label => label.Name)
            .Select(LabelMappings.ToDto())
            .ToListAsync(cancellationToken);
}
