using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WorkBoard.Application.Services;
using WorkBoard.Application.UseCases.Activities.ListActivities;
using WorkBoard.Application.UseCases.Auth.Login;
using WorkBoard.Application.UseCases.Auth.Logout;
using WorkBoard.Application.UseCases.Auth.RefreshToken;
using WorkBoard.Application.UseCases.Auth.Register;
using WorkBoard.Application.UseCases.Boards.AddColumn;
using WorkBoard.Application.UseCases.Boards.GetBoard;
using WorkBoard.Application.UseCases.Boards.MoveColumn;
using WorkBoard.Application.UseCases.Boards.RemoveColumn;
using WorkBoard.Application.UseCases.Boards.UpdateColumn;
using WorkBoard.Application.UseCases.Comments.AddComment;
using WorkBoard.Application.UseCases.Comments.DeleteComment;
using WorkBoard.Application.UseCases.Comments.GetComment;
using WorkBoard.Application.UseCases.Comments.ListComments;
using WorkBoard.Application.UseCases.Comments.UpdateComment;
using WorkBoard.Application.UseCases.Dashboard.GetDashboard;
using WorkBoard.Application.UseCases.Labels.CreateLabel;
using WorkBoard.Application.UseCases.Labels.DeleteLabel;
using WorkBoard.Application.UseCases.Labels.ListLabels;
using WorkBoard.Application.UseCases.Labels.UpdateLabel;
using WorkBoard.Application.UseCases.Notifications.CountUnreadNotifications;
using WorkBoard.Application.UseCases.Notifications.ListNotifications;
using WorkBoard.Application.UseCases.Notifications.MarkAllNotificationsRead;
using WorkBoard.Application.UseCases.Notifications.MarkNotificationRead;
using WorkBoard.Application.UseCases.Projects.AddBoard;
using WorkBoard.Application.UseCases.Projects.ArchiveProject;
using WorkBoard.Application.UseCases.Projects.CreateProject;
using WorkBoard.Application.UseCases.Projects.GetProject;
using WorkBoard.Application.UseCases.Projects.ListProjects;
using WorkBoard.Application.UseCases.Projects.RestoreProject;
using WorkBoard.Application.UseCases.Projects.UpdateProject;
using WorkBoard.Application.UseCases.Search.SearchWorkspace;
using WorkBoard.Application.UseCases.Tasks.ApplyTaskLabel;
using WorkBoard.Application.UseCases.Tasks.AssignTask;
using WorkBoard.Application.UseCases.Tasks.CreateTask;
using WorkBoard.Application.UseCases.Tasks.DeleteTask;
using WorkBoard.Application.UseCases.Tasks.GetTask;
using WorkBoard.Application.UseCases.Tasks.ListTasks;
using WorkBoard.Application.UseCases.Tasks.MoveTask;
using WorkBoard.Application.UseCases.Tasks.RemoveTaskLabel;
using WorkBoard.Application.UseCases.Tasks.UnassignTask;
using WorkBoard.Application.UseCases.Tasks.UpdateTask;
using WorkBoard.Application.UseCases.Workspaces.AddWorkspaceMember;
using WorkBoard.Application.UseCases.Workspaces.ArchiveWorkspace;
using WorkBoard.Application.UseCases.Workspaces.ChangeMemberRole;
using WorkBoard.Application.UseCases.Workspaces.CreateWorkspace;
using WorkBoard.Application.UseCases.Workspaces.GetWorkspace;
using WorkBoard.Application.UseCases.Workspaces.ListMyWorkspaces;
using WorkBoard.Application.UseCases.Workspaces.ListWorkspaceMembers;
using WorkBoard.Application.UseCases.Workspaces.RemoveWorkspaceMember;
using WorkBoard.Application.UseCases.Workspaces.UpdateWorkspace;
using WorkBoard.Application.Validators.Auth;

namespace WorkBoard.Application;

/// <summary>
/// Registers the application layer: one use case per operation, plus the services they share.
/// </summary>
/// <remarks>
/// <para>
/// Every use case is registered <em>as its interface</em>. An endpoint depends on
/// <c>ICreateTaskUseCase</c>, never on <c>CreateTaskUseCase</c>, which is what makes the dependency
/// point inwards at an abstraction the application layer owns rather than at a class it happens to
/// contain — and what lets a use case that calls another substitute it in a unit test.
/// </para>
/// <para>
/// Use cases are plain classes resolved from the container, not MediatR requests. The indirection a
/// mediator adds pays for itself when there are many cross-cutting behaviours or genuinely decoupled
/// publishers and subscribers; here there is one pipeline concern (validation), it is applied by the
/// endpoint filter, and every call has exactly one caller. What that indirection would cost is real:
/// navigating to a handler stops being "go to definition", and a typo in a request type becomes a
/// runtime resolution failure rather than a compile error. See ADR-010, and ADR-014 for why the
/// operations are nonetheless split one class per use case.
/// </para>
/// <para>
/// The list is written out rather than discovered by assembly scanning. It is long, and that is the
/// point: this file is the inventory of everything the application can do, and an operation that is
/// not on it does not exist. A scan would hide a missing registration until the first request that
/// needed it, and would hide a duplicate entirely.
/// </para>
/// <para>
/// Everything is scoped, because everything depends transitively on the request-scoped
/// <c>DbContext</c>, the current user and the tenant. Registering one as a singleton would capture a
/// disposed context — the classic captive-dependency bug, and one the container will not warn about.
/// </para>
/// </remarks>
public static class DependencyInjection
{
    /// <summary>Adds the use cases, the shared application services and the validators.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddApplicationServices();

        services.AddAuthUseCases();
        services.AddWorkspaceUseCases();
        services.AddProjectUseCases();
        services.AddBoardUseCases();
        services.AddTaskUseCases();
        services.AddCollaborationUseCases();
        services.AddOverviewUseCases();

        services.AddValidation();

        return services;
    }

    /// <summary>
    /// Rules that more than one use case needs, and that are worth having in one place.
    /// </summary>
    /// <remarks>
    /// These are the only application services left. A helper used by exactly one use case belongs
    /// inside it as a private method; promoting it to a service would be an abstraction with one
    /// caller, which is a file to open rather than a decision to understand.
    /// </remarks>
    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenIssuer, TokenIssuer>();
        services.AddScoped<ITaskRelationsService, TaskRelationsService>();
        services.AddScoped<IColumnRebalancer, ColumnRebalancer>();
        services.AddScoped<ILabelNameGuard, LabelNameGuard>();

        return services;
    }

    private static IServiceCollection AddAuthUseCases(this IServiceCollection services)
    {
        services.AddScoped<IRegisterUseCase, RegisterUseCase>();
        services.AddScoped<ILoginUseCase, LoginUseCase>();
        services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
        services.AddScoped<ILogoutUseCase, LogoutUseCase>();

        return services;
    }

    private static IServiceCollection AddWorkspaceUseCases(this IServiceCollection services)
    {
        services.AddScoped<IListMyWorkspacesUseCase, ListMyWorkspacesUseCase>();
        services.AddScoped<IGetWorkspaceUseCase, GetWorkspaceUseCase>();
        services.AddScoped<IListWorkspaceMembersUseCase, ListWorkspaceMembersUseCase>();
        services.AddScoped<ICreateWorkspaceUseCase, CreateWorkspaceUseCase>();
        services.AddScoped<IUpdateWorkspaceUseCase, UpdateWorkspaceUseCase>();
        services.AddScoped<IArchiveWorkspaceUseCase, ArchiveWorkspaceUseCase>();
        services.AddScoped<IAddWorkspaceMemberUseCase, AddWorkspaceMemberUseCase>();
        services.AddScoped<IChangeMemberRoleUseCase, ChangeMemberRoleUseCase>();
        services.AddScoped<IRemoveWorkspaceMemberUseCase, RemoveWorkspaceMemberUseCase>();

        return services;
    }

    private static IServiceCollection AddProjectUseCases(this IServiceCollection services)
    {
        services.AddScoped<IListProjectsUseCase, ListProjectsUseCase>();
        services.AddScoped<IGetProjectUseCase, GetProjectUseCase>();
        services.AddScoped<ICreateProjectUseCase, CreateProjectUseCase>();
        services.AddScoped<IUpdateProjectUseCase, UpdateProjectUseCase>();
        services.AddScoped<IArchiveProjectUseCase, ArchiveProjectUseCase>();
        services.AddScoped<IRestoreProjectUseCase, RestoreProjectUseCase>();
        services.AddScoped<IAddBoardUseCase, AddBoardUseCase>();

        return services;
    }

    private static IServiceCollection AddBoardUseCases(this IServiceCollection services)
    {
        services.AddScoped<IGetBoardUseCase, GetBoardUseCase>();
        services.AddScoped<IAddColumnUseCase, AddColumnUseCase>();
        services.AddScoped<IUpdateColumnUseCase, UpdateColumnUseCase>();
        services.AddScoped<IMoveColumnUseCase, MoveColumnUseCase>();
        services.AddScoped<IRemoveColumnUseCase, RemoveColumnUseCase>();

        return services;
    }

    private static IServiceCollection AddTaskUseCases(this IServiceCollection services)
    {
        services.AddScoped<IListTasksUseCase, ListTasksUseCase>();
        services.AddScoped<IGetTaskUseCase, GetTaskUseCase>();
        services.AddScoped<ICreateTaskUseCase, CreateTaskUseCase>();
        services.AddScoped<IUpdateTaskUseCase, UpdateTaskUseCase>();
        services.AddScoped<IMoveTaskUseCase, MoveTaskUseCase>();
        services.AddScoped<IAssignTaskUseCase, AssignTaskUseCase>();
        services.AddScoped<IUnassignTaskUseCase, UnassignTaskUseCase>();
        services.AddScoped<IApplyTaskLabelUseCase, ApplyTaskLabelUseCase>();
        services.AddScoped<IRemoveTaskLabelUseCase, RemoveTaskLabelUseCase>();
        services.AddScoped<IDeleteTaskUseCase, DeleteTaskUseCase>();

        return services;
    }

    private static IServiceCollection AddCollaborationUseCases(this IServiceCollection services)
    {
        services.AddScoped<IListCommentsUseCase, ListCommentsUseCase>();
        services.AddScoped<IGetCommentUseCase, GetCommentUseCase>();
        services.AddScoped<IAddCommentUseCase, AddCommentUseCase>();
        services.AddScoped<IUpdateCommentUseCase, UpdateCommentUseCase>();
        services.AddScoped<IDeleteCommentUseCase, DeleteCommentUseCase>();

        services.AddScoped<IListLabelsUseCase, ListLabelsUseCase>();
        services.AddScoped<ICreateLabelUseCase, CreateLabelUseCase>();
        services.AddScoped<IUpdateLabelUseCase, UpdateLabelUseCase>();
        services.AddScoped<IDeleteLabelUseCase, DeleteLabelUseCase>();

        services.AddScoped<IListNotificationsUseCase, ListNotificationsUseCase>();
        services.AddScoped<ICountUnreadNotificationsUseCase, CountUnreadNotificationsUseCase>();
        services.AddScoped<IMarkNotificationReadUseCase, MarkNotificationReadUseCase>();
        services.AddScoped<IMarkAllNotificationsReadUseCase, MarkAllNotificationsReadUseCase>();

        return services;
    }

    private static IServiceCollection AddOverviewUseCases(this IServiceCollection services)
    {
        services.AddScoped<IGetDashboardUseCase, GetDashboardUseCase>();
        services.AddScoped<IListActivitiesUseCase, ListActivitiesUseCase>();
        services.AddScoped<ISearchWorkspaceUseCase, SearchWorkspaceUseCase>();

        return services;
    }

    private static IServiceCollection AddValidation(this IServiceCollection services)
    {
        // Picks up every AbstractValidator in this assembly. Scanning is used here, unlike for the
        // use cases, because validators are uniform, are resolved by the type they validate rather
        // than by name, and an architecture test already asserts that every request type has one.
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>(ServiceLifetime.Singleton);

        // FluentValidation ships localised default messages and picks the language from the server
        // culture. That makes the API contract depend on the machine it runs on: the same request
        // returns English from a CI container and Portuguese from a developer laptop, and the
        // resource lookup mangles non-ASCII characters on a non-UTF-8 code page on top of that.
        // Error messages are part of the contract, so they are pinned to the invariant English set;
        // anything user-facing is spelled out explicitly in the validators anyway.
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        return services;
    }
}
