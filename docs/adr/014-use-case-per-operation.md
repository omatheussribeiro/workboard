# 014 — The canonical Clean Architecture layout, one class per use case

**Status:** Accepted · **Date:** 2026-09-17 · **Amends:** [ADR-013](013-write-repositories-alongside-the-context.md)

## The problem

The application layer was organised by feature: `Features/Tasks/{Dtos,Enums,Mapping,Validators,Services}`,
with each feature exposing an `ITaskQueryService` and an `ITaskCommandService`. That is a coherent
layout — vertical slices, everything about tasks in one place — and it was not wrong.

It had two problems, one small and one that matters.

The small one: `ITaskCommandService` had eight methods and nine constructor dependencies. Six of those
dependencies existed for one method each. A caller that only needed "delete a task" was handed the
ability to move, assign, relabel and rewrite one, and a unit test for any single rule had to
construct all nine substitutes.

The one that matters: the layout was ours. Someone opening the repository — a reviewer, an
interviewer, a new contributor — had to read it before they could navigate it. The layered layout
that most .NET Clean Architecture projects use (`Domain/{Entities,Enums,Events,Exceptions,Interfaces}`,
`Application/{UseCases,DTOs,Interfaces,Validators,Mappings,Services}`,
`Infrastructure/{Persistence/{Context,Configurations,Migrations,Repositories},Services,Authentication,DependencyInjection}`,
`Api/{Endpoints,Middleware,Filters,Extensions}`) is instantly legible to anyone who has seen one
before. For a codebase whose purpose is to be read, familiarity is a feature.

## Options considered

**Keep the feature folders.** Defensible, and the argument for vertical slices is real: a change to
tasks touches one directory. But it optimises for the person changing the code, and this repository
is read far more often than it is changed.

**Adopt the layout with MediatR.** The canonical template pairs `UseCases/Tasks/CreateTask/` with
`CreateTaskCommand` + `IRequestHandler`. It would give the folder structure for free. It would also
reverse [ADR-010](010-no-mediatr.md), whose reasoning has not changed: one pipeline concern, no
decoupled publishers, and "go to definition" stops working on every call.

**Adopt the layout, keep plain injected classes.** One sealed class per operation, one interface
each, a single `ExecuteAsync`, resolved from the container by the endpoint that needs it.

## The decision

The third. Fifty-one use cases, each in `UseCases/<Aggregate>/<Operation>/`, each with an interface
next to it. Supporting types moved to the folders the layout names: `DTOs/<Aggregate>/`,
`Validators/<Aggregate>/`, `Mappings/<Aggregate>/`, and a flat `Interfaces/` holding every outbound
abstraction — the repositories, `IUnitOfWork`, `IClock`, `ICurrentUser`, `ITenantContext`,
`ITokenService`, `IPasswordHasher`.

`Services/` holds the four rules that genuinely belong to more than one use case: `ITokenIssuer`
(registration, login and refresh return identical responses), `ITaskRelationsService` (the
workspace-membership check that assignment depends on, which is a security control rather than a
convenience), `IColumnRebalancer`, and `ILabelNameGuard`. A helper used by exactly one use case
stayed a private method inside it — promoting it would be an abstraction with one caller, which is a
file to open rather than a decision to understand.

The tests follow the same split: `Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`,
`IntegrationTests`, each referencing only the layer it exercises. `Domain.Tests` references the domain
alone, so a rule that ever needs EF Core to exercise stops the project compiling.

## What it bought

**Each use case declares exactly what it needs.** `DeleteTaskUseCase` takes two dependencies;
`CreateTaskUseCase` takes eight, and those eight are visibly what creating a card involves.

**The directory listing is the inventory.** `UseCases/Tasks/` lists the ten things that can happen to
a card. Four architecture tests keep it that way: one class per use case, one public `ExecuteAsync`,
a folder named after the operation, and no use case taking both `IWorkBoardDbContext` and
`IUnitOfWork` — which is how the read/write split from ADR-013 survives the reorganisation.

**Domain namespaces collapsed from nine to five.** `WorkBoard.Domain.{Entities,Enums,Events,
Exceptions,Interfaces}` replaced one namespace per aggregate, and the domain events that were tacked
onto the end of `WorkItem.cs` and `Workspace.cs` are now thirteen files in `Events/`.

## The trade-offs accepted

**A hundred and two files where there were twenty-two.** Each is small, but "find every place a task
can be modified" is now a directory listing rather than one file, and a change that spans two
operations touches two folders.

**`AddApplication` is two hundred lines of registrations.** It was tempting to scan the assembly.
Writing them out keeps the file honest as an inventory, and a test builds the container and compares
it against the assembly, so a missing entry fails the build rather than the first request.

**Cohesion moved.** Everything about tasks is no longer in one directory: the DTOs, the validators,
the mappings and the use cases are in four. That is the cost of the layered layout, and it is paid so
that someone who has never seen this repository knows where to look.

**Four folders from the reference layout are absent**, because inventing empty directories to match a
diagram is cargo cult: `Domain/ValueObjects` (this model uses guarded primitives; `ColorHex` and
`ProjectKey` are the candidates if that changes), `Application/Behaviors` (a MediatR pipeline concept
— validation runs as an endpoint filter, see ADR-010), `Infrastructure/{Cache,Integrations}` (there is
no cache and no external service), and `Api/Controllers` (minimal APIs, see the endpoint files).
