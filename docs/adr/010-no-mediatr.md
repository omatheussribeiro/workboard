# 010 — Plain injected handlers instead of a mediator

**Status:** Accepted · **Date:** 2026-09-17

## The problem

The application layer needs somewhere for the use cases to live, and a way for the API to reach them.

## Options considered

**MediatR.** The default in .NET clean-architecture templates. Each use case is a request and a
handler, dispatched through `IMediator`, with cross-cutting behaviours in a pipeline.

**Plain classes registered in the container.** `AuthService`, `TaskService`, `BoardService` — injected
into endpoints and called directly.

**Static functions.** No lifetime management, and no way to inject the `DbContext` without threading
it through every signature.

## Decision

Plain classes.

A mediator earns its keep when there are several cross-cutting behaviours to compose, or genuinely
decoupled publishers and subscribers. Here there is one pipeline concern — validation — and it is
applied by an endpoint filter that is visible in the endpoint's own definition. Every call has exactly
one caller.

What the indirection would cost is concrete:

- **"Go to definition" stops working.** Navigating from an endpoint to its logic becomes a search for
  the handler of a request type.
- **Wiring errors move from compile time to run time.** A missing registration is a resolution failure
  at the first request, not a build failure.
- **A request class plus a handler class per operation**, where a method on an existing service says
  the same thing.
- **A dependency** to keep current, now with a commercial licence for the versions after 12.

## What is kept from the pattern

The parts that were doing the work anyway:

- **Feature folders.** `Features/Tasks` holds its contracts, validators and service together.
- **Explicit contracts.** `CreateTaskRequest`, `MoveTaskRequest` and so on are real types with
  validators, not loose parameters.
- **A thin transport layer.** Endpoints bind, call one method and return. No logic.
- **In-process events where they are genuinely decoupled** — `DomainEventListener`, which has two
  implementations that know nothing about each other. That is the case a mediator is for, and it is
  implemented directly in twenty lines.

## When this would be revisited

If three or four behaviours needed to wrap every call — authorisation, caching, transactions,
retries — composing them by hand around each handler would become worse than a pipeline. That is the
signal to reconsider, and it has not appeared.

## Trade-offs accepted

- **Cross-cutting concerns are applied per endpoint**, so one can be forgotten. An architecture test
  asserts every request contract has a validator, which covers the concern that exists today.
- **It is not what a reviewer expects from a "clean architecture" repository.** That is the point of
  writing it down: the decision was made, not overlooked.
