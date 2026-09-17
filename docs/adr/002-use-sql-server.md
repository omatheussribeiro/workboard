# 002 — SQL Server, with domain-generated UUIDv7 keys

**Status:** Accepted · **Date:** 2026-09-17

## The problem

The data is relational and the constraints matter: a task belongs to exactly one project, a project
key is unique within its workspace, a membership is unique per user and workspace. Reads are a mix of
point lookups, ordered scans within a partition (a board column) and aggregates (the dashboard). The
application needs filtered indexes, covering indexes and a concurrency token it does not have to
maintain by hand.

## Options considered

**PostgreSQL.** Free, excellent, and the default recommendation for good reasons. Partial indexes,
`jsonb`, and a sane `uuid` sort order.

**SQL Server.** The same feature set under different names — filtered indexes, computed columns,
`rowversion` — plus the fact that the target for this kind of work is overwhelmingly a .NET shop
already running it.

**A document store.** Rejected quickly: the model is relational, and the isolation guarantee this
application is built around is a foreign key and an indexed predicate.

## Decision

SQL Server 2022, through `Microsoft.EntityFrameworkCore.SqlServer`.

`rowversion` is the deciding feature. It is maintained by the engine, so optimistic concurrency needs
no application code to bump a version column, and EF adds it to the `WHERE` clause of every `UPDATE`
automatically. Filtered indexes (`WHERE [IsRead] = 0`) keep the hot indexes small.

The provider is isolated: only `WorkBoard.Infrastructure` references it, and an architecture test
asserts that. Moving to PostgreSQL would be a provider swap plus a review of the raw index
definitions.

## Identity: UUIDv7, generated in the domain

Keys are `Guid.CreateVersion7()`, assigned by the entity constructor rather than by the database.

**Why not `IDENTITY`?** An aggregate would then be invalid until saved: its id is unknown, so a
domain event cannot carry it and a whole object graph cannot be built and asserted on without a round
trip. Client-generated ids make the domain independent of persistence.

**Why v7 and not `Guid.NewGuid()`?** Version 7 puts a millisecond timestamp in the leading bytes, so
ids sort chronologically wherever they are compared as bytes — in application code, in a log, in any
other store.

**The part that is usually claimed and is wrong.** UUIDv7 does *not* give clustered-index locality on
SQL Server. SQL Server does not compare `uniqueidentifier` byte by byte: it compares the last six
bytes first, then works backwards. The timestamp v7 places in the *first* six bytes is the last thing
it looks at, so a v7 key is as randomly ordered to SQL Server as `Guid.NewGuid()`. Claiming otherwise
is a common and expensive mistake.

What is done about it, on the three tables where it would hurt:

| Table | Primary key | Clustered on | Why |
| --- | --- | --- | --- |
| `Tasks` | non-clustered `Id` | `(ProjectId, Number)` unique | Strictly ascending per project, and it is also the uniqueness rule for the WB-42 key. |
| `Activities` | non-clustered `Id` | `(WorkspaceId, OccurredAtUtc, Id)` | Append-only per tenant; the feed reads it backwards, which costs nothing. |
| `Notifications` | non-clustered `Id` | `(RecipientId, CreatedAtUtc, Id)` | One user's inbox is read together and appended to together. |

Everything else keeps a clustered `Guid` primary key: those tables are small enough that the page
splits are not worth a second index to avoid.

One more consequence, found by running the application rather than by reading about it: EF treats a
`Guid` key as `ValueGeneratedOnAdd` and infers entity state from whether the key is set. An untracked
instance reached through a navigation with a key already assigned looks like an existing row, so EF
issues an `UPDATE` that matches nothing and throws `DbUpdateConcurrencyException` — with the symptom
appearing as "logging in fails with a concurrency conflict". The fix is one model-wide convention
declaring every `Guid` key `ValueGeneratedNever`, in `WorkBoardDbContext.ApplyConventions`.

## Trade-offs accepted

- **16 bytes per key instead of 4.** Wider indexes and wider foreign keys. Accepted for ids that are
  unguessable, globally unique and safe to generate anywhere.
- **Licensing.** Developer Edition locally and in CI; a production deployment needs a licence or
  Azure SQL. PostgreSQL would not.
- **Sort order surprises.** Ordering by a `Guid` column in SQL is not chronological. Every query that
  wants time order says so explicitly — see `ActivityService.ListAsync`.

## Related

[001](001-use-entity-framework-core.md), [008](008-optimistic-concurrency.md).
