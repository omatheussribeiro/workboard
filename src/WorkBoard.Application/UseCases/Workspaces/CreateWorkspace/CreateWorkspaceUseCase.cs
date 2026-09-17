using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.UseCases.Workspaces.CreateWorkspace;

/// <inheritdoc cref="ICreateWorkspaceUseCase"/>
public sealed class CreateWorkspaceUseCase(
    IWorkspaceRepository workspaces,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICreateWorkspaceUseCase
{
    /// <inheritdoc/>
    public async Task<WorkspaceSummaryDto> ExecuteAsync(
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var slug = request.Slug.Trim().ToLowerInvariant();

        if (await workspaces.SlugExistsAsync(slug, cancellationToken))
        {
            throw new ConflictException($"The slug '{slug}' is already in use.");
        }

        var workspace = Workspace.Create(request.Name, slug, userId);
        workspace.Describe(request.Description);

        workspaces.Add(workspace);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Built by hand rather than re-queried. The counts are known exactly — a workspace is born
        // with its creator and nothing else — so a round trip would confirm what the aggregate
        // already states.
        return new WorkspaceSummaryDto(
            workspace.Id,
            workspace.Name,
            workspace.Slug,
            workspace.Description,
            WorkspaceRole.Owner,
            MemberCount: 1,
            ProjectCount: 0,
            workspace.CreatedAtUtc);
    }
}
