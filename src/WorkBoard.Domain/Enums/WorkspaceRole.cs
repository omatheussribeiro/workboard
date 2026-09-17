
namespace WorkBoard.Domain.Enums;

/// <summary>
/// Membership level inside a single workspace.
/// </summary>
/// <remarks>
/// Stored as an <c>int</c> and ordered deliberately, so "at least admin" is a comparison rather
/// than a set membership test. Values are fixed and sparse: renumbering would silently re-grant
/// existing rows, and gaps leave room for levels between the current ones.
/// </remarks>
public enum WorkspaceRole
{
    /// <summary>Read/write on content, cannot change membership or workspace settings.</summary>
    Member = 0,

    /// <summary>Everything a member can do, plus membership, projects and labels.</summary>
    Admin = 10,

    /// <summary>The creator. Exactly one per workspace; cannot be removed or demoted.</summary>
    Owner = 20,
}
