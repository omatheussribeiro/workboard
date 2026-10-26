using System.Reflection;
using FluentValidation;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.ArchitectureTests;

/// <summary>
/// The shape the application layer is organised into, asserted rather than described.
/// </summary>
/// <remarks>
/// These rules are what stop the folder structure from being decoration. A use case that quietly
/// grows a second public method, a DTO that lands next to the use case that returns it, a read that
/// takes a unit of work — each is a small, reasonable-looking edit, and each erodes the separation
/// the layout exists to express.
/// </remarks>
public sealed class ApplicationStructureTests
{
    private static readonly Assembly Application = typeof(IUnitOfWork).Assembly;

    private static IEnumerable<Type> UseCases() =>
        Application.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Name.EndsWith("UseCase", StringComparison.Ordinal));

    /// <summary>
    /// Every use case is reachable through an interface.
    /// </summary>
    /// <remarks>
    /// This is what makes the endpoints depend on an abstraction the application layer owns, and what
    /// lets a use case that calls another substitute it in a unit test — which several of them do.
    /// </remarks>
    [Fact]
    public void Every_use_case_implements_a_matching_interface()
    {
        var offenders = UseCases()
            .Where(useCase => useCase.GetInterface($"I{useCase.Name}") is null)
            .Select(useCase => useCase.Name)
            .ToList();

        offenders.Should().BeEmpty("use cases are registered and consumed as interfaces, never as concrete types");
    }

    /// <summary>
    /// One operation per use case, named the same way everywhere.
    /// </summary>
    /// <remarks>
    /// A single <c>ExecuteAsync</c> is what makes "one class per use case" mean something. The moment
    /// a second public method appears the class is a service again, and the folder it lives in stops
    /// describing what it does.
    /// </remarks>
    [Fact]
    public void Every_use_case_exposes_exactly_one_operation_called_ExecuteAsync()
    {
        var offenders = UseCases()
            .Select(useCase => new
            {
                useCase.Name,
                Methods = useCase
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(method => !method.IsSpecialName)
                    .Select(method => method.Name)
                    .ToList(),
            })
            .Where(candidate => candidate.Methods is not ["ExecuteAsync"])
            .Select(candidate => $"{candidate.Name}: {string.Join(", ", candidate.Methods)}")
            .ToList();

        offenders.Should().BeEmpty();
    }

    /// <summary>
    /// The folder a use case lives in is named after the use case.
    /// </summary>
    /// <remarks>
    /// <c>UseCases/Tasks/CreateTask/CreateTaskUseCase.cs</c>, and nothing else. It is what makes the
    /// directory listing an inventory of the application's behaviour rather than a place to look
    /// things up.
    /// </remarks>
    [Fact]
    public void Every_use_case_lives_in_a_folder_named_after_it()
    {
        var offenders = UseCases()
            .Where(useCase =>
            {
                var operation = useCase.Name[..^"UseCase".Length];
                var folder = useCase.Namespace?.Split('.').LastOrDefault();
                return !string.Equals(folder, operation, StringComparison.Ordinal);
            })
            .Select(useCase => $"{useCase.Namespace}.{useCase.Name}")
            .ToList();

        offenders.Should().BeEmpty();
    }

    /// <summary>
    /// A use case either reads or writes, never both.
    /// </summary>
    /// <remarks>
    /// Reads use query interfaces; writes load aggregates through repositories and commit through
    /// <see cref="IUnitOfWork"/>. A class holding both can
    /// compose any query it likes on the write path, which is how the aggregate-loading rules ended
    /// up duplicated in the first place — see ADR-013.
    /// </remarks>
    [Fact]
    public void No_use_case_takes_both_a_query_interface_and_the_unit_of_work()
    {
        var offenders = UseCases()
            .Where(useCase =>
            {
                var parameters = useCase.GetConstructors()
                    .SelectMany(constructor => constructor.GetParameters())
                    .Select(parameter => parameter.ParameterType)
                    .ToList();

                return parameters.Any(parameter => parameter.IsInterface
                                                   && parameter.Namespace == "WorkBoard.Application.Interfaces"
                                                   && parameter.Name.EndsWith("Queries", StringComparison.Ordinal))
                       && parameters.Contains(typeof(IUnitOfWork));
            })
            .Select(useCase => useCase.Name)
            .ToList();

        offenders.Should().BeEmpty();
    }

    /// <summary>Request and response contracts live under <c>DTOs</c>.</summary>
    [Fact]
    public void Contracts_live_in_the_DTOs_namespace()
    {
        var offenders = Application.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Name.EndsWith("Request", StringComparison.Ordinal)
                           || type.Name.EndsWith("Dto", StringComparison.Ordinal))
            .Where(type => type.Namespace?.StartsWith("WorkBoard.Application.DTOs", StringComparison.Ordinal) != true)
            .Select(type => $"{type.Namespace}.{type.Name}")
            .ToList();

        offenders.Should().BeEmpty("a contract in the wrong folder is a contract nobody finds when reviewing the API");
    }

    /// <summary>Validators live under <c>Validators</c>, and mappings under <c>Mappings</c>.</summary>
    [Fact]
    public void Validators_and_mappings_live_in_their_own_namespaces()
    {
        var misplacedValidators = Application.GetTypes()
            .Where(type => type.BaseType is { IsGenericType: true } baseType
                           && baseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .Where(type => type.Namespace?.StartsWith("WorkBoard.Application.Validators", StringComparison.Ordinal) != true)
            .Select(type => type.Name);

        var misplacedMappings = Application.GetTypes()
            .Where(type => type.Name.EndsWith("Mappings", StringComparison.Ordinal))
            .Where(type => type.Namespace?.StartsWith("WorkBoard.Application.Mappings", StringComparison.Ordinal) != true)
            .Select(type => type.Name);

        misplacedValidators.Concat(misplacedMappings).Should().BeEmpty();
    }

    [Fact]
    public void Application_has_no_entity_framework_dependency()
    {
        var referencesEf = Application.GetReferencedAssemblies()
            .Any(assembly => assembly.Name?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true);

        referencesEf.Should().BeFalse("EF Core queries and persistence helpers belong to Infrastructure");
    }

    /// <summary>
    /// Every inbound contract is validated.
    /// </summary>
    /// <remarks>
    /// "Request" is the naming convention for a body contract. Query-string shapes and responses are
    /// excluded: the first are bound as individual parameters, the second are never inbound.
    /// </remarks>
    [Fact]
    public void Every_request_contract_has_a_validator()
    {
        var validated = Application.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.BaseType is { IsGenericType: true } baseType
                           && baseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .Select(type => type.BaseType!.GetGenericArguments()[0])
            .ToHashSet();

        var unvalidated = Application.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Name.EndsWith("Request", StringComparison.Ordinal))
            .Where(type => type.Namespace?.StartsWith("WorkBoard.Application.DTOs", StringComparison.Ordinal) == true)

            // DTOs/Common holds shapes shared by every feature rather than request bodies. PageRequest
            // is the only one ending in "Request", and it validates itself: its constructor clamps
            // both values, so there is no unvalidated path through it to exempt.
            .Where(type => type.Namespace != "WorkBoard.Application.DTOs.Common")
            .Where(type => !validated.Contains(type))
            .Select(type => type.Name)
            .ToList();

        // Deliberate exceptions: these carry only ids the route already constrains to a Guid, or an
        // enum the binder rejects when unknown, so there is nothing left for a validator to check.
        var exempt = new[] { "ChangeMemberRoleRequest", "AssignTaskRequest", "MoveColumnRequest", "RefreshRequest" };

        unvalidated.Except(exempt).Should().BeEmpty(
            "an unvalidated request contract reaches the domain with whatever the caller sent");
    }
}
