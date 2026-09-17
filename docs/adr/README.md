# Architectural Decision Records

Each file records one decision: the problem that forced it, the options that were real, what was
chosen, and what was given up. They are written in the past tense and are not edited when the world
changes — a decision that is superseded gets a new record that says so, because the reasoning behind
a choice stays useful even after the choice is reversed.

The format is deliberately short. An ADR nobody reads is worth nothing, and the ones that get read
are the ones that fit on a screen.

| # | Decision | Status |
| --- | --- | --- |
| [001](001-use-entity-framework-core.md) | Entity Framework Core, without a repository layer | Amended by 013 |
| [002](002-use-sql-server.md) | SQL Server, with domain-generated UUIDv7 keys | Accepted |
| [003](003-use-angular.md) | Angular with standalone components and signals | Accepted |
| [004](004-use-modular-monolith.md) | A modular monolith with workspace-scoped multi-tenancy | Accepted |
| [005](005-use-github-actions.md) | GitHub Actions for CI and CD | Accepted |
| [006](006-use-jwt.md) | JWT access tokens with rotating refresh tokens | Accepted |
| [007](007-name-the-task-entity-workitem.md) | The task entity is called `WorkItem` in C# | Accepted |
| [008](008-optimistic-concurrency.md) | Optimistic concurrency on tasks and projects | Accepted |
| [009](009-exceptions-over-result-types.md) | Exceptions rather than a `Result<T>` type | Accepted |
| [010](010-no-mediatr.md) | Plain injected handlers instead of a mediator | Accepted |
| [011](011-domain-events-in-the-transaction.md) | Domain events dispatched inside the transaction | Accepted |
| [012](012-migrations-in-the-pipeline.md) | Migrations applied by the pipeline, not at startup | Accepted |
| [013](013-write-repositories-alongside-the-context.md) | Aggregate repositories for writes, projections for reads | Amended by 014 |
| [014](014-use-case-per-operation.md) | The canonical Clean Architecture layout, one class per use case | Accepted |
