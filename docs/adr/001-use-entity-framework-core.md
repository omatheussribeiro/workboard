# 001 — Entity Framework Core, without a repository layer

**Status:** Accepted · **Date:** 2026-09-17

## The problem

The application needs to persist a graph with real structure: workspaces own projects, projects own
boards and tasks, tasks own comments, assignments and labels. The write side has to load an
aggregate, let it enforce its invariants and save the result as one transaction. The read side has
to answer very differently-shaped questions — a whole Kanban board in one request, six aggregates
for a dashboard, a filtered and paged task list — without dragging entire tables into memory.

Those two sides want different things from a data-access layer, and most of the pain people
attribute to ORMs comes from serving them both the same way.

## Options considered

**Dapper, or raw ADO.NET.** Fastest, and total control over the SQL. It also means hand-writing every
insert, update and delete for fourteen entities, hand-maintaining the mapping as the model changes,
and giving up change tracking — so saving a modified aggregate becomes the caller's problem. The
schema would have to be managed separately, by hand or with a second tool.

**EF Core behind a generic `IRepository<T>`.** The shape most people expect. It hides the
`DbContext`, which sounds like it protects the application from the persistence choice. In practice
it does the opposite: `IQueryable` leaks through the interface anyway, or the interface returns
`IEnumerable` and every non-anticipated query becomes in-memory filtering. Projections, `GroupBy`,
paging and split queries either need a method each or are impossible. The abstraction ends up being
a worse `DbSet` with more code.

**EF Core exposed through a narrow context interface.** The application layer depends on
`IWorkBoardDbContext`, which exposes `DbSet<T>` and `SaveChangesAsync` and nothing else.

## Decision

EF Core, with the third option.

`IWorkBoardDbContext` gives the application the full expressiveness of LINQ — projections, grouping,
paging, correlated subqueries — while keeping it off the concrete context and off the SQL Server
provider. The architecture tests enforce that, and tests can substitute a context without a database.

The two sides are then written differently, on purpose:

- **Writes** load the aggregate tracked, with `Include` where an invariant spans a collection, call a
  domain method, and save. `BoardService.RequireWithColumnsAsync` is the shape.
- **Reads** never materialise an entity. They project straight to a DTO, so EF reads only the listed
  columns and tracks nothing. `BoardService.GetAsync` is the shape.

## What was gained

- Migrations. The schema is generated from the model and versioned in the repository, rather than
  maintained as a parallel set of scripts that drift.
- Change tracking, so a write path is "load, mutate, save" and the UPDATE statement is derived.
- Relationships configured once in Fluent API, including the delete behaviour and the indexes.
- LINQ that composes, so a filter added to a query is a clause added to one SQL statement.

## What was given up, and what is done about it

**Generated SQL is not hand-written SQL.** It is close enough on everything this application does,
and where it is not, the escape hatch is `FromSql` — which is still EF, so the result materialises
normally.

**Easy to write badly.** The three ways are all present in this codebase as examples of what was
avoided, documented at the call site: a cartesian explosion from stacked `Include`s (see the remarks
on `BoardService.GetAsync`), N+1 from lazy loading (the Proxies package is not referenced at all),
and tracking on read paths (every read says `AsNoTracking` or projects).

**A global tracking default is a trap.** Setting `QueryTrackingBehavior.NoTracking` globally looks
like a free win and silently breaks every write path — `SaveChanges` persists what the tracker knows
about, so the domain method runs and nothing reaches the database. The default is left at `TrackAll`
and the cost is paid per read query, where it is visible.

**Bulk operations bypass the tracker.** `ExecuteUpdateAsync` is used for exactly one thing — marking
notifications read — and the comment there says why that is acceptable: the operation carries no
domain meaning, so skipping domain events costs nothing.

## Related

[004](004-use-modular-monolith.md) on the query filter this interface carries,
[008](008-optimistic-concurrency.md) on concurrency tokens,
[012](012-migrations-in-the-pipeline.md) on how migrations reach production.
