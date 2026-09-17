# 011 — Domain events dispatched inside the transaction

**Status:** Accepted · **Date:** 2026-09-17

## The problem

Creating a task has to produce three things: the task, an activity entry recording that it was
created, and notifications for anyone assigned. Moving a card produces two. If any of those can
succeed while the others fail, the history stops describing the data — and a history that sometimes
lies is worse than no history.

## Options considered

**Write everything inline in the handler.** `TaskService.CreateAsync` adds the task, then the
activity, then the notifications. Correct, and every handler that touches a task has to remember to
do it, forever.

**Dispatch events after `SaveChanges`.** The obvious implementation, and it breaks exactly where it
matters: the task commits, the process dies, the activity never exists. The failure is silent and
permanent.

**A transactional outbox.** The event is written to an outbox table in the same transaction, and a
background worker delivers it. The standard answer when events leave the process.

**Dispatch before `SaveChanges`, so handlers add to the same unit of work.**

## Decision

The last one.

Aggregates raise events (`TaskCreated`, `TaskMoved`, `TaskFieldChanged`). `WorkBoardDbContext`
overrides `SaveChangesAsync` and, *before* calling the base implementation, drains them through
`DomainEventDispatcher`. Listeners add `Activity` and `Notification` entities to the same context, so
they join the same unit of work and therefore the same implicit transaction. One `SaveChanges`, one
commit, all or nothing.

Two listeners, deliberately separate: `ActivityWriter` answers "what happened", `NotificationWriter`
answers "who needs to know". They react to the same events for different reasons, and only one of them
will grow an email transport later.

## Why not an outbox

An outbox solves the dual-write problem: a change in the database and a message to another system,
where the two cannot share a transaction. Neither listener here leaves the process — both write to
tables in this same database, which the transaction already covers.

Adding one would mean an outbox table, a background worker, a delivery-tracking mechanism and
eventual consistency, to solve a problem that does not exist. If notifications become emails, the
outbox goes in precisely here, and the listeners do not change.

## Details that are easy to get wrong

- **Events are cleared before dispatch, not after.** A listener that touches the same aggregate would
  otherwise see the event it is currently handling and reprocess it on the next pass.
- **The queue is drained, not processed once.** A listener can cause further events; the loop runs
  until none remain, with a cap that turns an accidental cycle into a clear exception instead of a
  hang.
- **Listeners receive the `DbContext`.** They run inside `SaveChangesAsync`; resolving their own would
  be a second transaction, able to commit while the change that caused it rolls back.
- **`NotificationWriter` projects rather than materialises.** Loading entities mid-`SaveChanges` would
  add them to a change tracker that is already in flight.

## Trade-offs accepted

- **A slow listener slows the request**, because it is inside it. Both listeners do one insert each.
- **A listener that throws rolls back the whole operation.** For an activity entry that is correct —
  the alternative is the silent divergence this decision exists to prevent.
- **Events do not survive the process.** Nothing outside the database consumes them, so there is
  nothing to survive for. That changes the day something does, and so does this decision.
- **`ExecuteUpdateAsync` bypasses all of it.** Used once, for marking notifications read, where no
  domain event would be raised anyway. The comment at that call site says so.

## Related

[001](001-use-entity-framework-core.md) on the unit of work this relies on.
