namespace WorkBoard.Api.Contracts;

/// <summary>Adds an extra board to an existing project.</summary>
/// <param name="Name">Board name.</param>
public sealed record CreateBoardRequest(string Name);
