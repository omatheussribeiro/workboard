# Isolate EF Core read queries in Infrastructure

* Status: Accepted
* Amends: [ADR-001](001-use-entity-framework-core.md), [ADR-013](013-write-repositories-alongside-the-context.md), [ADR-014](014-use-case-per-operation.md)

## Context

The application layer had direct access to `DbSet<T>` through `IWorkBoardDbContext`. This kept the
SQL Server provider out of Application, but still coupled use cases to EF Core and kept persistence
helpers and a few projections beside application code. That made the layer boundary depend on an
interface that exposed the persistence framework itself.

## Decision

Application use cases depend on narrow, feature-level query interfaces that return application
DTOs. Infrastructure implements those interfaces and owns EF Core query composition, persistence
projections, paging helpers, and mappings that require database context. Application does not
reference EF Core.

Write use cases continue to use aggregate repositories and `IUnitOfWork`. Tenant filters remain
configured on the Infrastructure `DbContext`; tenant-sensitive query behavior and the explicit
workspace-resolution exceptions are preserved.

## Consequences

Use cases no longer compose persistence queries. Query adapters must preserve the existing SQL
projections, filters, sorting, and paging semantics. The extra query interfaces provide a seam for
Application tests without adding a generic repository or a query framework. Infrastructure remains
the only layer that needs EF Core.
