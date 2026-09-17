using System.Linq.Expressions;
using WorkBoard.Application.DTOs.Labels;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Mappings.Labels;

/// <summary>Entity-to-DTO projection for labels.</summary>
public static class LabelMappings
{
    /// <summary>Projects a label, counting its uses with a subquery rather than loading the join rows.</summary>
    public static Expression<Func<Label, LabelDto>> ToDto() =>
        label => new LabelDto(label.Id, label.Name, label.ColorHex, label.Tasks.Count);
}
