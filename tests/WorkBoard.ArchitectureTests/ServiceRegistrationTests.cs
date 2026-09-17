using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkBoard.Application;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.DependencyInjection;

namespace WorkBoard.ArchitectureTests;

/// <summary>
/// Asserts that the container can actually build what the layers declare.
/// </summary>
/// <remarks>
/// A missing registration is a runtime failure on the first request that needs it, which in practice
/// means it is found by whoever opens that page rather than by the build. With fifty-one use cases
/// written out by hand in <c>AddApplication</c>, that is not a theoretical risk — so the inventory is
/// checked against the assembly here.
/// </remarks>
public sealed class ServiceRegistrationTests
{
    private static readonly Assembly Application = typeof(IWorkBoardDbContext).Assembly;

    private static ServiceCollection BuildContainer()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=(localdb)\\MSSQLLocalDB;Database=WorkBoard-Arch",
                ["Jwt:SigningKey"] = new string('k', 48),
            })
            .Build();

        services.AddApplication();
        services.AddInfrastructure(configuration);

        return services;
    }

    [Fact]
    public void Every_use_case_interface_is_registered()
    {
        var registered = BuildContainer().Select(descriptor => descriptor.ServiceType).ToHashSet();

        var declared = Application.GetTypes()
            .Where(type => type.IsInterface)
            .Where(type => type.Name.EndsWith("UseCase", StringComparison.Ordinal));

        var missing = declared.Where(type => !registered.Contains(type)).Select(type => type.Name).ToList();

        missing.Should().BeEmpty("an unregistered use case fails at the first request that needs it, not at build time");
    }

    [Fact]
    public void Every_repository_interface_is_registered()
    {
        var registered = BuildContainer().Select(descriptor => descriptor.ServiceType).ToHashSet();

        var declared = Application.GetTypes()
            .Where(type => type.IsInterface)
            .Where(type => type.Namespace == "WorkBoard.Application.Interfaces")
            .Where(type => type.Name.EndsWith("Repository", StringComparison.Ordinal));

        var missing = declared.Where(type => !registered.Contains(type)).Select(type => type.Name).ToList();

        missing.Should().BeEmpty();
    }

    [Fact]
    public void Every_shared_application_service_is_registered()
    {
        var registered = BuildContainer().Select(descriptor => descriptor.ServiceType).ToHashSet();

        var declared = Application.GetTypes()
            .Where(type => type.IsInterface)
            .Where(type => type.Namespace == "WorkBoard.Application.Services");

        var missing = declared.Where(type => !registered.Contains(type)).Select(type => type.Name).ToList();

        missing.Should().BeEmpty();
    }

    /// <summary>
    /// Nothing that depends on the request-scoped context may be a singleton.
    /// </summary>
    /// <remarks>
    /// The captive-dependency bug: a singleton holding a scoped <c>DbContext</c> keeps the first
    /// request's context alive for the lifetime of the process, and every subsequent request writes
    /// through a disposed object. The container does not warn about it by default.
    /// </remarks>
    [Fact]
    public void Use_cases_repositories_and_services_are_scoped()
    {
        var offenders = BuildContainer()
            .Where(descriptor => descriptor.ServiceType.Namespace?.StartsWith("WorkBoard.", StringComparison.Ordinal) == true)
            .Where(descriptor => descriptor.ServiceType.Name.EndsWith("UseCase", StringComparison.Ordinal)
                                 || descriptor.ServiceType.Name.EndsWith("Repository", StringComparison.Ordinal)
                                 || descriptor.ServiceType.Namespace == "WorkBoard.Application.Services")
            .Where(descriptor => descriptor.Lifetime == ServiceLifetime.Singleton)
            .Select(descriptor => descriptor.ServiceType.Name)
            .ToList();

        offenders.Should().BeEmpty("anything reaching the DbContext must follow the request scope");
    }
}
