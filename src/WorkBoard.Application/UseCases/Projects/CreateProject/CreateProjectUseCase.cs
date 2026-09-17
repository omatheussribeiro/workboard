using WorkBoard.Application.DTOs.Projects;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.UseCases.Projects.CreateProject;

/// <inheritdoc cref="ICreateProjectUseCase"/>
/// <remarks>
/// <para>
/// Project, board and its three columns are inserted in one <c>SaveChangesAsync</c>, which EF Core
/// wraps in a single transaction. A project that exists without a board would be a dead end in the
/// UI, so partial success is not an acceptable outcome here.
/// </para>
/// <para>
/// The tenant id appears here and nowhere on the read side, and the asymmetry is the point: a global
/// query filter constrains what a query returns, not what an insert may write. A new row has to be
/// stamped with the workspace explicitly, and <see cref="ITenantContext.RequireWorkspaceId"/> throws
/// rather than defaulting, so a route that forgot to resolve a workspace cannot create orphans.
/// </para>
/// </remarks>
public sealed class CreateProjectUseCase(
    IProjectRepository projects,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    ICurrentUser currentUser) : ICreateProjectUseCase
{
    /// <inheritdoc/>
    public async Task<ProjectSummaryDto> ExecuteAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var workspaceId = tenant.RequireWorkspaceId();
        var key = request.Key.Trim().ToUpperInvariant();

        if (await projects.KeyExistsAsync(key, cancellationToken))
        {
            throw new ConflictException($"A project with key '{key}' already exists in this workspace.");
        }

        var project = Project.CreateWithDefaultBoard(workspaceId, request.Name, key, currentUser.RequireUserId());
        project.Describe(request.Description);

        projects.Add(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProjectSummaryDto(
            project.Id,
            project.Name,
            project.Key,
            project.Description,
            IsArchived: false,
            TaskCount: 0,
            OpenTaskCount: 0,
            project.Boards.First().Id,
            project.CreatedAtUtc);
    }
}
