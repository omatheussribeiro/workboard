# 012 — Migrations applied by the pipeline, not at startup

**Status:** Accepted · **Date:** 2026-09-17

## The problem

The schema is code-first, so it changes with the code. Something has to apply those changes to each
environment, at the right moment, exactly once.

## Options considered

**`Database.Migrate()` at startup.** One line, no deployment step, and the default in most samples.
It is also a trap, for reasons that only appear in production:

- During a rolling deploy it takes a schema lock while old instances are still serving requests.
- It runs once per replica, so several instances race for the same migration.
- The application process needs permission to alter schema, which it otherwise would not.
- A failed migration becomes a crash loop rather than a deployment that stopped with an error.
- There is no opportunity to review the SQL before it runs.

**`EnsureCreated`.** Creates the schema from the model and skips migrations entirely, so the
migration history is never exercised. A broken migration would pass every test and fail on deploy.
Not used anywhere, including in the integration tests, which call `MigrateAsync` for this reason.

**An idempotent script applied as a deployment step.**

## Decision

Both, gated by configuration rather than by environment name.

`Database:MigrateOnStartup` and `Database:SeedDemoData` are flags. Development and `docker compose`
set them true; `appsettings.Production.json` sets them false. Flags rather than an environment check,
so an integration test or a demo deployment can enable them without pretending to be Development —
and so production cannot get them by accident.

For anything real, the pipeline generates a script and applies it before the new version rolls out:

```bash
dotnet ef migrations script \
  --idempotent \
  --project src/WorkBoard.Infrastructure \
  --startup-project src/WorkBoard.Infrastructure \
  --output artifacts/migrate.sql
```

`--idempotent` wraps each migration in a check against `__EFMigrationsHistory`, so the script is safe
to run twice — which matters when a deployment is retried.

## Why this ordering works

Migrations are written to be backward compatible with the currently deployed version, so the sequence
is: apply the schema change, then roll out the code. A column addition is safe; a rename is done as
add, backfill, deploy, remove — across two releases.

That constraint is what makes zero-downtime deployment possible at all, and it is a constraint on how
migrations are written, not something the tooling provides.

## Trade-offs accepted

- **A deployment step that can be forgotten**, producing an application whose schema is behind its
  code. The release notes on every GitHub Release say so explicitly.
- **Two code paths.** `DatabaseInitializer` exists for Development and is dead code in production.
  It is thirty lines and it is what makes `docker compose up` work with no setup.
- **Destructive changes need care.** Dropping a column the previous version still writes to will fail
  it. The add-backfill-remove sequence above is the answer, and it takes two releases.
- **The seeded Development passwords are public.** Intentionally, and guarded by a flag Production
  never sets.

## Related

[005](005-use-github-actions.md), [002](002-use-sql-server.md).
