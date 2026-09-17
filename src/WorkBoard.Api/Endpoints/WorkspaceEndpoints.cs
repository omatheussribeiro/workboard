using WorkBoard.Api.Filters;
using WorkBoard.Application.DTOs.Workspaces;
using WorkBoard.Application.UseCases.Workspaces.AddWorkspaceMember;
using WorkBoard.Application.UseCases.Workspaces.ArchiveWorkspace;
using WorkBoard.Application.UseCases.Workspaces.ChangeMemberRole;
using WorkBoard.Application.UseCases.Workspaces.CreateWorkspace;
using WorkBoard.Application.UseCases.Workspaces.GetWorkspace;
using WorkBoard.Application.UseCases.Workspaces.ListMyWorkspaces;
using WorkBoard.Application.UseCases.Workspaces.ListWorkspaceMembers;
using WorkBoard.Application.UseCases.Workspaces.RemoveWorkspaceMember;
using WorkBoard.Application.UseCases.Workspaces.UpdateWorkspace;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Endpoints;

/// <summary>
/// Workspaces and their membership.
/// </summary>
/// <remarks>
/// Split into three groups on purpose. The collection routes ("my workspaces", "create one") cannot
/// require the <c>WorkspaceMember</c> policy — there is no workspace in the route yet — so they
/// require only authentication and are scoped by the caller's own user id inside the use case.
/// Everything addressed by <c>{workspaceId}</c> goes through a policy, which is what binds the
/// request to a tenant; administrative changes go through the stricter one.
/// </remarks>
public static class WorkspaceEndpoints
{
    /// <summary>Maps the workspace routes.</summary>
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        MapCollection(app);
        MapMemberScoped(app);
        MapAdminScoped(app);

        return app;
    }

    private static void MapCollection(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces")
            .WithTags("Workspaces")
            .RequireAuthorization();

        group.MapGet("/", async (IListMyWorkspacesUseCase listMine, CancellationToken cancellationToken) =>
                Results.Ok(await listMine.ExecuteAsync(cancellationToken)))
            .WithName("ListMyWorkspaces")
            .WithSummary("Lists the workspaces the caller belongs to.")
            .Produces<IReadOnlyList<WorkspaceSummaryDto>>();

        group.MapPost("/", async (
                CreateWorkspaceRequest request,
                ICreateWorkspaceUseCase create,
                CancellationToken cancellationToken) =>
            {
                var created = await create.ExecuteAsync(request, cancellationToken);
                return Results.Created($"/api/workspaces/{created.Id}", created);
            })
            .WithValidation<CreateWorkspaceRequest>()
            .WithName("CreateWorkspace")
            .WithSummary("Creates a workspace owned by the caller.")
            .Produces<WorkspaceSummaryDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapMemberScoped(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces/{workspaceId:guid}")
            .WithTags("Workspaces")
            .RequireAuthorization(WorkBoardClaims.WorkspaceMemberPolicy);

        group.MapGet("/", async (
                Guid workspaceId,
                IGetWorkspaceUseCase get,
                CancellationToken cancellationToken) =>
            Results.Ok(await get.ExecuteAsync(workspaceId, cancellationToken)))
            .WithName("GetWorkspace")
            .Produces<WorkspaceSummaryDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/members", async (
                Guid workspaceId,
                IListWorkspaceMembersUseCase listMembers,
                CancellationToken cancellationToken) =>
            Results.Ok(await listMembers.ExecuteAsync(workspaceId, cancellationToken)))
            .WithName("ListWorkspaceMembers")
            .Produces<IReadOnlyList<WorkspaceMemberDto>>();
    }

    /// <summary>
    /// Administrative routes.
    /// </summary>
    /// <remarks>
    /// The same handler with a higher minimum role, so membership is resolved — and the tenant bound
    /// — in exactly one place whichever policy applies.
    /// </remarks>
    private static void MapAdminScoped(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces/{workspaceId:guid}")
            .WithTags("Workspaces")
            .RequireAuthorization(WorkBoardClaims.WorkspaceAdminPolicy);

        group.MapPut("/", async (
                Guid workspaceId,
                UpdateWorkspaceRequest request,
                IUpdateWorkspaceUseCase update,
                CancellationToken cancellationToken) =>
            {
                await update.ExecuteAsync(workspaceId, request, cancellationToken);
                return Results.NoContent();
            })
            .WithValidation<UpdateWorkspaceRequest>()
            .WithName("UpdateWorkspace")
            .Produces(StatusCodes.Status204NoContent);

        group.MapDelete("/", async (
                Guid workspaceId,
                IArchiveWorkspaceUseCase archive,
                CancellationToken cancellationToken) =>
            {
                await archive.ExecuteAsync(workspaceId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("ArchiveWorkspace")
            .WithSummary("Archives the workspace. Owner only; nothing is deleted.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/members", async (
                Guid workspaceId,
                AddMemberRequest request,
                IAddWorkspaceMemberUseCase addMember,
                CancellationToken cancellationToken) =>
            Results.Ok(await addMember.ExecuteAsync(workspaceId, request, cancellationToken)))
            .WithValidation<AddMemberRequest>()
            .WithName("AddWorkspaceMember")
            .Produces<WorkspaceMemberDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/members/{userId:guid}", async (
                Guid workspaceId,
                Guid userId,
                ChangeMemberRoleRequest request,
                IChangeMemberRoleUseCase changeRole,
                CancellationToken cancellationToken) =>
            {
                await changeRole.ExecuteAsync(workspaceId, userId, request, cancellationToken);
                return Results.NoContent();
            })
            .WithName("ChangeWorkspaceMemberRole")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/members/{userId:guid}", async (
                Guid workspaceId,
                Guid userId,
                IRemoveWorkspaceMemberUseCase removeMember,
                CancellationToken cancellationToken) =>
            {
                await removeMember.ExecuteAsync(workspaceId, userId, cancellationToken);
                return Results.NoContent();
            })
            .WithName("RemoveWorkspaceMember")
            .Produces(StatusCodes.Status204NoContent);
    }
}
