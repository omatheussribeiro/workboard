# 007 — The task entity is called `WorkItem` in C#

**Status:** Accepted · **Date:** 2026-09-17

## The problem

The central concept of the product is a task. The obvious class name, `Task`, collides with
`System.Threading.Tasks.Task` — which appears in the signature of essentially every method in an
async codebase.

## Options considered

**Call it `Task` and live with it.** Every file that mentions both needs an alias
(`using DomainTask = WorkBoard.Domain.Tasks.Task;`) or fully-qualified names. `Task<Task>` is a real
return type under this scheme. Reviewers stop being able to read signatures at a glance, and the
compiler's error messages become unhelpful.

**Call it `TaskItem`.** No collision, but the name is a tautology that appears nowhere in the product,
the database or the API.

**Call it `Issue`, `Ticket` or `Card`.** Each is a real word from a real tool, and each is *a
different concept* — an issue is reported, a ticket is assigned, a card is a view. Renaming the domain
concept to dodge a CLR collision means the code and the product stop speaking the same language.

**Call it `WorkItem` in C# only, and map the name back at the single boundary where it matters.**

## Decision

The CLR type is `WorkItem`. Everything else says "task": the table is `Tasks`, the API routes are
`/api/workspaces/{id}/tasks`, the DTOs are `TaskDetail` and `TaskListItem`, and the UI says task.

The translation happens in exactly one line, in `WorkItemConfiguration`:

```csharp
builder.ToTable("Tasks");
```

## Why this is the right trade

The alternative costs a disambiguation on hundreds of lines. This costs one line, plus a reader's
momentary surprise on first encountering the type — which the XML documentation on `WorkItem` answers
immediately.

`WorkItem` is also not an invented word: it is the term Azure DevOps uses for the same concept, so it
reads as domain vocabulary rather than as a workaround.

## Consequences

- Someone searching the C# for "Task" finds `System.Threading.Tasks.Task` first. Searching for
  "WorkItem" finds the entity. Both are learnable in a minute.
- The `DbSet` is deliberately still named `Tasks`, so LINQ reads `db.Tasks.Where(...)` — the queries
  speak the product's language even though the element type does not.
- Nothing outside `WorkBoard.Domain.Tasks` and the EF configuration needs to know the name differs.
