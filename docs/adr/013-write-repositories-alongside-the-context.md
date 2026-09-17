# 013 — Aggregate repositories for writes, projections for reads

**Status:** Accepted · **Date:** 2026-09-17 · **Amends:** [ADR-001](001-use-entity-framework-core.md)

## The problem

ADR-001 rejected a repository layer and gave the application services `IWorkBoardDbContext`
directly. That decision was right about the read side and, as the codebase grew, wrong about a
detail on the write side.

Every service that modified a task opened with some version of this:

```csharp
var task = await db.Tasks
    .Include(t => t.Assignments)
    .Include(t => t.Labels)
    .Include(t => t.ParentTask)
    .FirstOrDefaultAsync(t => t.Id == taskId, ct)
    ?? throw new NotFoundException("Task", taskId);
```

There were four near-identical copies of it, and they had already drifted: one omitted `Labels`.
That omission is not a compile error and not a crash. `WorkItem.ApplyLabel` checks whether the label
is already applied, so against an unloaded collection it decides "no" and inserts a duplicate — which
fails at the unique index, as a 500, from a code path that looks correct.

The same shape appeared elsewhere. `IgnoringTenantFilter` — the escape hatch from the multi-tenancy
filter, and the single most security-sensitive call in the codebase — was spread across `AuthService`
and `WorkspaceService`, each with a comment explaining why it was safe there. Every new service was
one `Include` and one filter bypass away from a bug that reads like something else.

## Options considered

**Leave it, and rely on review.** The comments explaining each `IgnoringTenantFilter` were good, and
the duplication was only four copies. But the failure mode is silent, and the number of copies only
grows.

**A generic `IRepository<T>`.** Rejected again, for the reasons in ADR-001, and they have not
changed: it cannot express the board projection, the dashboard aggregates or the filtered task list
without either leaking `IQueryable` — at which point it is a worse `DbSet` — or returning
`IEnumerable` and moving the work into memory.

**A repository per aggregate, covering writes only.** Each method exists because loading that
aggregate *for modification* has one correct shape. Reads stay on the context, inside query services,
where LINQ still composes into a single statement.

## The decision

The third option. Concretely:

- `Abstractions/Persistence/` declares one interface per aggregate root — `ITaskRepository`,
  `IProjectRepository`, `IBoardRepository`, `IWorkspaceRepository`, `IUserRepository`,
  `ILabelRepository`, `ICommentRepository`, `INotificationRepository` — plus `IUnitOfWork`.
- There is no base interface and no `IRepository<T>`. The eight interfaces share no members, because
  the operations they name are not the same operations.
- Repositories never save. They stage; a service decides the unit of work is complete and commits
  once through `IUnitOfWork`. That is what keeps "create a task" — which bumps the project counter,
  inserts the card, its assignments, its labels, and the activity rows the domain-event dispatcher
  produces — inside one transaction rather than five.
- Every service is split into `IXQueryService` and `IXCommandService`. Commands take repositories;
  queries take `IWorkBoardDbContext`. An architecture test fails the build if a command service takes
  the context.
- Mapping lives in `Mapping/XMappings.cs` as `Expression<Func<TEntity, TDto>>`, so a projection is
  written once and reused by the list, the detail and the response of every write that returns one.

## What it bought

**The filter bypass is now confined to two repositories.** `UserRepository` (accounts exist before
workspaces do) and `WorkspaceRepository` (slugs are globally unique), plus the two places that
resolve a tenant in the first place. The allow-list in
`TenantIsolationConventionTests` is the enforcement, and it got shorter rather than longer.

**The aggregate-loading rules exist once.** `FindForUpdateAsync` names the correct `Include` set for
each aggregate, with a comment saying which invariant needs which collection.

**The command services became unit-testable.** They were not before: exercising "a column from
another project is rejected" meant standing up a provider and seeding four tables, so nobody did.
With substituted repositories it is three lines of arrangement, and
`TaskCommandServiceTests` now covers the rules a database would not check for you.

## The trade-offs accepted

**Two more indirections between a service and its SQL.** Reading "what query does this run" now
means opening the repository. That is a real cost, paid on the write side only, where the queries are
uniform and small.

**The split is a convention held up by a test.** Nothing in the type system stops a query service
from calling `SaveChanges`. The architecture tests are what make it a rule.

**Two navigation properties had to become explicit results.** `column.Board.ProjectId` and
`token.User` are populated by EF and null on any object the domain built itself, which made the rules
using them impossible to test without a database. They are now returned as `ColumnPlacement` and
`RefreshTokenOwner` — one query each, still tracked, and a missing join is a compile error rather
than a `NullReferenceException` inside the refresh flow.

**No AutoMapper.** A convention-based mapper buys nothing when the source is a query rather than an
object: it cannot be handed to EF as a projection without `ProjectTo`, it costs a startup
configuration step and a runtime validation test, and it turns a rename into a failure found at
request time instead of at compile time. The expression-tree mappings are longer to read and
impossible to get silently wrong.
