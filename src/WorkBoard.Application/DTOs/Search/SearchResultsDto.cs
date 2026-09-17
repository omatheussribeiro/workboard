namespace WorkBoard.Application.DTOs.Search;

/// <summary>Search hits grouped by the kind of thing matched, which is how the palette renders them.</summary>
/// <param name="Tasks">Matching cards.</param>
/// <param name="Projects">Matching projects.</param>
/// <param name="Comments">Matching comments.</param>
public sealed record SearchResultsDto(
    IReadOnlyList<SearchHitDto> Tasks,
    IReadOnlyList<SearchHitDto> Projects,
    IReadOnlyList<SearchHitDto> Comments);
