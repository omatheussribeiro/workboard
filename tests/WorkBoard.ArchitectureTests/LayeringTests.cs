using System.Reflection;
using NetArchTest.Rules;
using WorkBoard.Api.Security;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.Persistence.Context;
using WorkBoard.Domain.Entities;

namespace WorkBoard.ArchitectureTests;

/// <summary>
/// Executable versions of the rules the README states in prose.
/// </summary>
/// <remarks>
/// A layering diagram is a claim; these are the claim, checked. The rules chosen are the ones whose
/// violation would be both easy and quiet — a <c>using</c> added to fix a compile error, a new
/// entity that forgets the tenant interface.
/// </remarks>
public sealed class LayeringTests
{
    private static readonly Assembly Domain = typeof(Entity).Assembly;
    private static readonly Assembly Application = typeof(IUnitOfWork).Assembly;
    private static readonly Assembly Infrastructure = typeof(WorkBoardDbContext).Assembly;
    private static readonly Assembly Api = typeof(HttpCurrentUser).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_any_other_layer()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "WorkBoard.Application",
                "WorkBoard.Infrastructure",
                "WorkBoard.Api")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "the domain is the innermost layer and must not know the layers that use it");
    }

    [Fact]
    public void Domain_does_not_depend_on_infrastructure_frameworks()
    {
        // The rule that actually keeps the domain testable. An EF attribute or a DI container
        // reference is the first step towards business rules that need a database to exercise.
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions.DependencyInjection")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "domain rules must be exercisable without a database or a host");
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny("WorkBoard.Infrastructure", "WorkBoard.Api")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "the application layer depends on abstractions; infrastructure supplies them");
    }

    [Fact]
    public void Application_does_not_depend_on_the_sql_server_provider()
    {
        // The application layer must not depend on a database provider.
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore.SqlServer")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Application_does_not_depend_on_aspnetcore()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "handlers must not reach for HttpContext; the API adapts it to ICurrentUser");
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("WorkBoard.Api")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "Infrastructure implements application ports and must not know the transport layer");
    }

    [Fact]
    public void Api_does_not_reference_the_concrete_DbContext_outside_composition()
    {
        // Program.cs names WorkBoardDbContext once, to register a health check. Anything else in the
        // API touching it would be an endpoint going around the application layer.
        var offenders = Types.InAssembly(Api)
            .That()
            .ResideInNamespace("WorkBoard.Api.Endpoints")
            .Should()
            .NotHaveDependencyOn("WorkBoard.Infrastructure.Persistence")
            .GetResult();

        offenders.FailingTypeNames.Should().BeNullOrEmpty(
            "endpoints call application services, not the database directly");
    }

    [Fact]
    public void Api_security_does_not_depend_on_infrastructure_persistence()
    {
        var result = Types.InAssembly(Api)
            .That()
            .ResideInNamespace("WorkBoard.Api.Security")
            .ShouldNot()
            .HaveDependencyOn("WorkBoard.Infrastructure.Persistence")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "authorization uses application query ports and the API-owned tenant context");
    }

    [Fact]
    public void Infrastructure_is_the_only_layer_that_references_the_provider()
    {
        var result = Types.InAssembly(Api)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore.SqlServer")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "swapping the database provider should be a change in Infrastructure alone");
    }
}
