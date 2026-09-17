# 003 — Angular with standalone components and signals

**Status:** Accepted · **Date:** 2026-09-17

## The problem

The client is a single-page application with a drag-and-drop board, forms with real validation,
route-level access control and a session that has to refresh itself without the user noticing. It has
to be maintainable by someone who did not write it.

## Options considered

**React.** The largest ecosystem, and nothing here it could not do. It also means choosing a router,
a data layer, a form library and a build setup, and those choices are what a reader has to reverse
engineer six months later.

**Blazor WebAssembly.** One language across the stack, and the domain types shared rather than
mirrored — genuinely attractive for a .NET project. The cost is a multi-megabyte runtime download, a
smaller component ecosystem, and a drag-and-drop story that means interop anyway.

**Angular.** Router, forms, HTTP client, DI and testing in the box, with one documented way to do
each. The CDK provides accessible drag-and-drop, which is the single hardest interaction here.

## Decision

Angular, current stable, using the modern surface throughout:

- **Standalone components.** No `NgModule`. A component declares what it imports, so its dependencies
  are visible in the file and lazy loading is per component rather than per module.
- **Signals.** All component state. The app is zoneless (`provideZonelessChangeDetection`), so
  `zone.js` is not shipped at all and change detection runs when a signal changes rather than after
  every timer and XHR the page happens to fire.
- **Reactive forms** where validation is real (login, register, project and workspace creation), and
  `ngModel` where a control is a filter with no validity rules to express.
- **Lazy loading** on every route. The initial bundle is the shell, the router and auth; the board
  pulls in the CDK only when someone opens a board.
- **Functional interceptors and guards**, which are plain functions taking `inject()` — no class, no
  provider entry, and trivially unit-testable.

## Trade-offs accepted

**More ceremony than React for a small screen.** A trivial component is more lines. The payoff is
that a non-trivial one — the board, the task detail — looks like every other non-trivial one.

**The types are hand-written, not generated from OpenAPI.** At this surface size a generator adds a
build step, a tool version to track and thousands of reviewable lines. The cost is that nothing
checks the TypeScript against the server: a contract change is caught by the integration tests and by
a failing screen rather than by `tsc`. At a larger surface that trade flips, and the API already
publishes the document a generator would consume.

**Zoneless is still relatively new.** A third-party library that assumes zone-based change detection
will not update the view. Nothing here uses one, and the CDK is zoneless-clean.

## Related

[006](006-use-jwt.md) for what the client does about token storage.
