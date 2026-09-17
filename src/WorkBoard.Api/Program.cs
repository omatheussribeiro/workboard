using Serilog;
using WorkBoard.Api.Extensions;
using WorkBoard.Api.Middleware;
using WorkBoard.Api.Security;
using WorkBoard.Application;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.DependencyInjection;
using WorkBoard.Infrastructure.Persistence.Context;

var builder = WebApplication.CreateBuilder(args);

// Serilog replaces the default logger before anything else can log, so startup output is structured
// too. Configuration lives in appsettings, which means changing a level in production is a config
// change rather than a rebuild.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "WorkBoard.Api")
    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

// Each of these is an extension method in Configuration/, one per concern, each carrying the
// explanation of why it is configured the way it is. Composition root as a table of contents:
// what the application is made of is visible here, and why is one "go to definition" away.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddWorkBoardAuthentication();
builder.Services.AddWorkBoardAuthorization();

builder.Services.AddWorkBoardRateLimiting();
builder.Services.AddWorkBoardJson();
builder.Services.AddWorkBoardCors(builder.Configuration);
builder.Services.AddWorkBoardOpenApi();
builder.Services.AddWorkBoardObservability(ThisAssembly.Version);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

builder.Services.AddHealthChecks().AddDbContextCheck<WorkBoardDbContext>("database");

var app = builder.Build();

app.UseWorkBoardPipeline();

await app.InitialiseDatabaseAsync();
await app.RunAsync();

/// <summary>
/// Exposed so <c>WebApplicationFactory</c> in the integration tests can find the entry point.
/// A top-level-statements program has an internal <c>Program</c> class, which the test host cannot see.
/// </summary>
public partial class Program;
