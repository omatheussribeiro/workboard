using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>Write-side access to workspace labels.</summary>
public interface ILabelRepository
{
    /// <summary>Loads a label tracked.</summary>
    Task<Label?> FindForUpdateAsync(Guid labelId, CancellationToken cancellationToken);

    /// <summary>Loads several labels tracked, in one query.</summary>
    Task<IReadOnlyList<Label>> ListByIdsAsync(IReadOnlyCollection<Guid> labelIds, CancellationToken cancellationToken);

    /// <summary>Whether the workspace already has a label with this name, ignoring <paramref name="exceptId"/>.</summary>
    Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>Stages a new label.</summary>
    void Add(Label label);

    /// <summary>Stages a deletion.</summary>
    void Remove(Label label);
}
