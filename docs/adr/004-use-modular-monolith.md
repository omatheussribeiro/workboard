# 004 — A modular monolith with workspace-scoped multi-tenancy

**Status:** Accepted · **Date:** 2026-09-17

## The problem

Two things had to be decided together, because the answer to each constrains the other: how the code
is split, and how one tenant's data is kept from another's.

The isolation requirement is absolute. A workspace's projects, tasks, comments and notifications must
be invisible to every other workspace — not "filtered in the usual case", but unreachable even when a
caller supplies a valid id belonging to someone else.

## Options considered, for the deployment shape

**Microservices.** Independent scaling and deployment. Also: a task, its activity entry and its
notifications would span three services, so creating a task becomes a distributed transaction or an
eventual-consistency problem, and the isolation guarantee has to be re-proven at every boundary. The
cost is enormous and the driver — independent scaling — does not exist here.

**A layered monolith with no internal structure.** Everything reachable from everything. Fast to
write, and after a year there is no boundary left to enforce.

**A modular monolith.** One deployable, one database, one transaction — with boundaries enforced by
project references and by tests rather than by the network.

## Options considered, for tenancy

**A database per tenant.** The strongest isolation there is: a query cannot cross a boundary that is a
separate connection string. It also means a migration run per tenant, connection pooling per tenant,
and cross-tenant reporting becoming a distributed query. Right at a hundred large customers, wrong
here.

**A schema per tenant.** Most of the operational cost of the above with a weaker guarantee.

**A shared schema with a discriminator column.** One database, a `WorkspaceId` on every tenant-scoped
row. Cheapest to run, and it lives or dies on whether the filter can be forgotten.

## Decision

A modular monolith, with a shared schema and a `WorkspaceId` discriminator — and the filter made
impossible to forget.

Four mechanisms, each covering a different failure:

**1. The tenant is bound by passing authorisation, not by middleware.** `WorkspaceRoleHandler` reads
the workspace id from the route, proves membership against `WorkspaceMembers`, and only then calls
`WorkspaceContext.Enter`. Because the same code does both, there is no path that reaches a handler
with a workspace set but unverified. Middleware would have to guess which routes are tenant-scoped
and would run before authorisation.

**2. The filter is applied by reflection, not per entity.** `WorkBoardDbContext.ApplyTenantFilters`
walks the model and attaches a query filter to every type implementing `IWorkspaceScoped`. A
per-entity `HasQueryFilter` would be something a new entity could be added without, and the omission
would be invisible. Here, implementing the interface is the only thing a new entity has to do — and
an architecture test asserts that any type with a `WorkspaceId` does.

**3. It fails closed.** The filter compares against `tenant.WorkspaceId ?? Guid.Empty`. A bug that
forgets to set the tenant makes queries return *nothing*, not everything.

**4. `WorkspaceId` is denormalised onto every tenant-scoped row.** A comment could derive its
workspace through task → project → workspace. Storing it directly means the filter is one indexed
predicate instead of a three-table join, and — more importantly — a query that forgets to join is
still filtered.

The escape hatch, `IgnoringTenantFilter<T>()`, exists for the two operations that genuinely cannot be
tenant-scoped: authenticating before any workspace is known, and resolving which workspaces a user
belongs to. An architecture test reads the source and fails if any file outside a reviewed allow-list
calls it.

## What this does not protect

A query filter constrains **reads, not writes**. Nothing stops an `INSERT` referencing an id from
another tenant. `TaskService.EnsureMembersAsync` is the explicit check for the one place that
matters — assigning a task — and there is an integration test that tries exactly that attack.

## Trade-offs accepted

- **A bug in the filter is a cross-tenant leak**, not a crash. That is why the isolation tests exist
  as a suite of their own, setting up two real tenants and trying every route from one to the other.
- **Noisy neighbours share resources.** One workspace generating heavy load affects the rest.
  Acceptable at this scale; the exit is to move a large tenant to its own database, which the
  `IWorkspaceScoped` boundary already makes possible.
- **`SELECT *` in a database console shows every tenant.** Operational access is not protected by the
  application's filter, and should be governed separately.
- **One database means one migration**, and a schema change affects every tenant at once.

## Related

[001](001-use-entity-framework-core.md), [006](006-use-jwt.md) on why membership is read per request
rather than carried in the token.
