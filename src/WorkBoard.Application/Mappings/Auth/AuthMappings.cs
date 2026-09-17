using WorkBoard.Application.DTOs.Auth;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Mappings.Auth;

/// <summary>
/// Maps a <see cref="User"/> onto the shape the client is allowed to see.
/// </summary>
/// <remarks>
/// A plain method rather than an expression tree, because the source here is an entity already
/// loaded for authentication rather than a query to translate. It exists as a named function for one
/// reason: <see cref="User"/> carries a password hash and an active flag, and every place that
/// serialises a user has to omit them. One conversion means one place to review, and a new sensitive
/// field added to the entity cannot leak by being picked up implicitly.
/// </remarks>
public static class AuthMappings
{
    /// <summary>Projects the authenticated user onto its transport shape.</summary>
    public static AuthenticatedUserDto ToDto(this User user) =>
        new(user.Id, user.Email, user.DisplayName, user.IsSystemAdmin);
}
