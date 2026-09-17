using System.Reflection;
using WorkBoard.Domain.Entities;
using WorkBoard.Domain.Interfaces;

namespace WorkBoard.ArchitectureTests;

/// <summary>Rules that keep the domain model a domain model rather than a bag of properties.</summary>
public sealed class DomainConventionTests
{
    private static readonly Assembly Domain = typeof(Entity).Assembly;

    [Fact]
    public void Domain_entities_keep_their_setters_private()
    {
        var offenders = Domain.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Entity)) || typeof(IWorkspaceScoped).IsAssignableFrom(type))
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetProperties().Select(property => (Type: type, Property: property)))
            .Where(candidate => candidate.Property.SetMethod is { IsPublic: true })
            .Select(candidate => $"{candidate.Type.Name}.{candidate.Property.Name}")
            .ToList();

        offenders.Should().BeEmpty(
            "state changes go through methods that enforce invariants, not through property setters");
    }

    [Fact]
    public void Domain_entities_are_sealed_or_abstract()
    {
        var offenders = Domain.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Entity)))
            .Where(type => type is { IsAbstract: false, IsSealed: false })
            .Select(type => type.Name)
            .ToList();

        offenders.Should().BeEmpty("entity inheritance is not part of this model; sealing says so");
    }
}
