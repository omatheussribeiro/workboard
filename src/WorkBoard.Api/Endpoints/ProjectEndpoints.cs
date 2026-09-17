using WorkBoard.Api.Contracts;
using WorkBoard.Api.Filters;
using WorkBoard.Application.DTOs.Projects;
using WorkBoard.Application.UseCases.Projects.AddBoard;
using WorkBoard.Application.UseCases.Projects.ArchiveProject;
using WorkBoard.Application.UseCases.Projects.CreateProject;
using WorkBoard.Application.UseCases.Projects.GetProject;
using WorkBoard.Application.UseCases.Projects.ListProjects;
using WorkBoard.Application.UseCases.Projects.RestoreProject;
using WorkBoard.Application.UseCases.Projects.UpdateProject;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Endpoints;

/// <summary>Projects inside a workspace.</summary>
public static class ProjectEndpoints
{
    private const string BasePath = "/api/workspaces/{workspaceId:guid}/projects";

    /// <summary>Maps the project routes.</summary>
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        MapMemberScoped(app);
        MapAdminScoped(app);

        return app;
    }

    private static void MapMemberScoped(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BasePath)
            .WithTags("Projects")
            .RequireAuthorization(WorkBoardClaims.WorkspaceMemberPolicy);

        group.MapGet("/", async (
                bool? includeArchived,
                IListProjectsUseCase list,
                CancellationToken cancellationToken) =>
            Results.Ok(await list.ExecuteAsync(includeArchived ?? false, cancellationToken)))
            .WithName("ListProjects")
            .Produces<IReadOnlyList<ProjectSummaryDto>>();

        group.MapGet("/{projectId:guid}", async (
                Guid projectId,
                IGetProjectUseCase get,
                CancellationToken cancellationToken) =>
            Results.Ok(await get.ExecuteAsync(projectId, cancellationToken)))
            .WithName("GetProject")
            .Produces<ProjectSummaryDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                Guid workspaceId,
                CreateProjectRequest request,
                ICreateProjectUseCase create,
                CancellationToken cancellationToken) =>
            {
                var created = await create.ExecuteAsync(request, cancellationToken);
                return Results.Created($"/api/workspaces/{workspaceId}/projects/{created.Id}", created);
            })
            .WithValidation<CreateProjectRequest>()
            .WithName("CreateProject")
            .WithSummary("Creates a project together with a default Kanban board.")
            .Produces<ProjectSummaryDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{projectId:guid}", async (
                Guid projectId,
                UpdateProjectRequest request,
                IUpdateProjectUseCase update,
                CancellationToken cancellationToken) =>
            {
                await update.ExecuteAsync(projectId, request, cancellationToken);
                return Results.NoContent();
            })
            .WithValidation<UpdateProjectRequest>()
            .WithName("UpdateProject")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/{projectId:guid}/boards", async (
                Guid projectId,
                CreateBoardRequest request,
                IAddBoardUseCase addBoard,
                CancellationToken cancellationToken) =>
            {
                var boardId = await addBoard.ExecuteAsync(projectId, request.Name, cancellationToken);
                return Results.Ok(new { boardId });
            })
            .WithName("AddBoard")
            .Produces(StatusCodes.Status200OK);
    }

    /// <summary>Archiving is an admin action: it hides a whole project from everyone in the workspace.</summary>
    private static void MapAdminScoped(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BasePath)
            .WithTags("Projects")
            .RequireAuthorization(WorkBoardClaims.WorkspaceAdminPolicy);

        group.MapDelete("/{projectId:guid}", async (
                Guid projectId,
                IArchiveProjectUseCase archive,
                CancellationToken cancellationToken) =>
            {
                await archive.ExecuteAsync(projectId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("ArchiveProject")
            .WithSummary("Archives a project. Nothing is deleted.")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/{projectId:guid}/restore", async (
                Guid projectId,
                IRestoreProjectUseCase restore,
                CancellationToken cancellationToken) =>
            {
                await restore.ExecuteAsync(projectId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("RestoreProject")
            .Produces(StatusCodes.Status204NoContent);
    }
}
