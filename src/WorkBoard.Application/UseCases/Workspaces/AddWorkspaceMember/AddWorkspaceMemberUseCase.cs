using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Workspaces.AddWorkspaceMember;

/// <inheritdoc cref="IAddWorkspaceMemberUseCase"/>
/// <remarks>
/// Membership is granted only to accounts that already exist; there is no invite-by-email flow that
/// would create a shell user. That keeps the user table free of rows nobody ever claims, and keeps
/// this endpoint from becoming a way to send mail to arbitrary addresses.
/// </remarks>
public sealed class AddWorkspaceMemberUseCase(
    IWorkspaceRepository workspaces,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IClock clock) : IAddWorkspaceMemberUseCase
{
    /// <inheritdoc/>
    public async Task<WorkspaceMemberDto> ExecuteAsync(
        Guid workspaceId,
        AddMemberRequest request,
        CancellationToken cancellationToken)
    {
        var workspace = await workspaces.FindForUpdateAsync(workspaceId, cancellationToken)
                        ?? throw new NotFoundException("Workspace", workspaceId);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (user is not { IsActive: true })
        {
            // A deactivated account is reported as missing. Saying "that account is disabled" would
            // confirm the address is registered to anyone who can add members.
            throw new NotFoundException("User", request.Email);
        }

        workspace.AddMember(user.Id, request.Role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkspaceMemberDto(user.Id, user.Email, user.DisplayName, request.Role, clock.UtcNow);
    }
}
