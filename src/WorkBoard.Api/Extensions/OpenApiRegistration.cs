namespace WorkBoard.Api.Extensions;

/// <summary>Registers the OpenAPI document.</summary>
public static class OpenApiRegistration
{
    /// <summary>Adds the document with its title and description.</summary>
    public static IServiceCollection AddWorkBoardOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new Microsoft.OpenApi.OpenApiInfo
            {
                Title = "WorkBoard API",
                Version = "v1",
                Description = "Project and task management. Every workspace-scoped route is isolated by tenant.",
            };

            return Task.CompletedTask;
        }));

        return services;
    }
}
