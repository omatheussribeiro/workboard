# 008 — Optimistic concurrency on tasks and projects

**Status:** Accepted · **Date:** 2026-09-17

## The problem

A Kanban board is a shared surface. Two people dragging the same card, or one editing a task while
another moves it, is routine rather than exceptional. Project task numbering has a sharper version of
the same problem: the per-project counter that produces WB-42 must not hand the same number to two
concurrent creations.

Without any control, the last write wins silently. Someone's change disappears and nobody is told.

## Options considered

**Nothing.** Last write wins. Cheapest, and the failure is invisible — which is what makes it the
worst option rather than merely the simplest.

**Pessimistic locking.** `SELECT ... WITH (UPDLOCK)` while a task is open. Correct, and wrong for a
UI: a card would be locked for as long as someone had its detail panel open, including while they
were at lunch.

**Optimistic concurrency with a version token.** Every read returns the version; every write carries
the version it was based on; the write fails if the row has changed since.

**A CRDT or operational transform.** The right answer for real-time collaborative editing. Vast
overkill for a card that moves a few times a day.

## Decision

Optimistic concurrency, using SQL Server's `rowversion` on `Tasks` and `Projects`.

`rowversion` is maintained by the engine, so nothing in the application has to remember to bump it.
EF adds it to the `WHERE` clause of every `UPDATE`; if the row has changed, zero rows match and EF
raises `DbUpdateConcurrencyException`, which `TaskService` translates into a 409 with a message
written for a user: *"This task was changed by someone else. Reload and try again."*

The token is part of the API contract. `TaskDetail` and every board card carry `rowVersion`, and
`UpdateTaskRequest` and `MoveTaskRequest` require it — a validator rejects a request without one, so
the check cannot be skipped by omission.

On `Projects` the token guards something less obvious: `TaskCounter`. Two simultaneous task creations
collide on the project row rather than both being handed WB-42, and the loser retries. The unique
index `(ProjectId, Number)` is the backstop if that ever fails.

## What the client does with a 409

The board applies a move optimistically and reloads from the server on failure, because after a
rejection it no longer knows what the truth is. Inverting the local change would be guessing.

The successful response carries a fresh token, and the client adopts it — without that, a second drag
of the same card sends the pre-move token and is rejected as stale. There is an integration test for
exactly that retry path.

## Trade-offs accepted

- **The client must round-trip a value it cannot interpret.** `rowVersion` is opaque base64 that the
  browser only stores and hands back.
- **A conflict is a user-visible failure.** That is the point: a visible retry beats a silent loss.
- **Only `Tasks` and `Projects` carry a token.** Comments are owned by their author, labels change
  rarely, and board columns are edited by admins. Adding tokens everywhere would cost eight bytes per
  row and a contract change per entity to solve a problem those tables do not have.
- **It protects a row, not a user's intent.** Two people editing different fields of the same task
  still conflict. Field-level merging is possible and is not worth it here.

## Related

[002](002-use-sql-server.md) — `rowversion` is a large part of why SQL Server was chosen.
