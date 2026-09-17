# 009 — Exceptions rather than a `Result<T>` type

**Status:** Accepted · **Date:** 2026-09-17

## The problem

Application handlers fail in a handful of predictable ways: the thing was not found, the caller is not
allowed, the request conflicts with current state, the domain refuses the operation. Each maps to a
different HTTP status. Something has to carry that from the handler to the endpoint.

## Options considered

**`Result<T>` with an error union.** Failure appears in the signature, so the compiler reminds the
caller it exists. The cost shows up in handlers that do several lookups in sequence: each becomes a
match-and-propagate step, and a handler that reads six things turns into a ladder of them. Without a
railway-oriented library the noise is considerable; with one, the codebase acquires a paradigm a new
reader has to learn before reading a CRUD handler.

**Exceptions with a single translation point.** Handlers throw; one `IExceptionHandler` maps to RFC
9457 problem details.

**A discriminated union.** C# does not have one yet. Approximations with `OneOf` or a hand-rolled
base class buy some of the benefit and add a dependency or a lot of boilerplate.

## Decision

Exceptions, mapped once in `ProblemDetailsExceptionHandler`.

Four application exceptions (`NotFoundException`, `ForbiddenException`, `ConflictException`, plus the
domain's own `DomainException`) and one handler that decides what each looks like on the wire:

| Thrown | Status | Meaning |
| --- | --- | --- |
| `ValidationException` (FluentValidation) | 400 | The request is malformed. |
| `NotFoundException` | 404 | Does not exist, or is invisible to this tenant. |
| `ForbiddenException` | 403 | Authenticated, not permitted. |
| `ConflictException` | 409 | Conflicts with current state. |
| `DomainException` | 422 | Well-formed, and the domain refuses it. |
| anything else | 500 | Logged with the correlation id; no detail returned. |

The 404-for-both-cases row is deliberate: a non-member and a non-existent workspace get the same
answer, because returning 403 for one and 404 for the other lets an outsider probe which ids exist.

The 422 distinction is the one that gets argued about. 400 means the server could not understand the
request; a deadline in the past is perfectly understandable and refused on business grounds, which is
what 422 is for.

## Trade-offs accepted, honestly

- **Failure is invisible in the signature.** `Task<TaskDetail>` does not say it can throw. This is the
  real cost, and it is mitigated by there being only four exception types, all in one namespace, all
  documented.
- **Exceptions are slower than a return value.** Roughly microseconds, on a path that already includes
  a database round trip. Irrelevant here, and it would matter in a hot loop.
- **Nothing forces a caller to handle them.** By design: nothing between the handler and the endpoint
  wants to, and the one place that does — the exception handler — handles all of them.

## What is not done with exceptions

Control flow. An exception here always means the request cannot proceed. Business outcomes that are
merely negative — a search with no results, an already-read notification — return normally.

## Related

[010](010-no-mediatr.md) — the same preference for one obvious path over an indirection.
