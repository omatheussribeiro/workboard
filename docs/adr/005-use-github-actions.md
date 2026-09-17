# 005 — GitHub Actions for CI and CD

**Status:** Accepted · **Date:** 2026-09-17

## The problem

Every push needs to build both halves of the application, run three backend test suites and the
frontend one, and prove the container images still build. Every version tag needs to publish two
multi-architecture images and cut a release. None of that should require standing up a build server.

## Options considered

**Azure DevOps Pipelines.** The natural pair for a .NET and SQL Server stack, with better native
support for test reporting. It also means a second account, a second place to look, and a pipeline
that lives away from the code it builds.

**Jenkins, or another self-hosted server.** Complete control, and a machine to patch, back up and
explain. Not justifiable for a project of this size.

**GitHub Actions.** Runs where the code already is, configured by files in the repository, with a
container registry attached.

## Decision

GitHub Actions, with the workflows in `.github/workflows/`.

`ci.yml` runs on push to `main` and on every pull request, in three jobs: backend, frontend, and a
container-image job gated on both. `cd.yml` runs on a `v*.*.*` tag: it re-verifies, publishes to the
GitHub Container Registry, then creates the release.

Decisions inside the workflows worth calling out:

- **The integration tests use Testcontainers, not a service container.** The runner already provides
  a Docker daemon, so the suite starts its own SQL Server. That keeps the dependency declared in code
  — the tests run identically on a laptop and in CI, rather than depending on a `services:` block the
  local run does not have.
- **CD re-runs the tests.** A tag can be pushed to any commit, including one that never passed CI.
  Trusting that it did would make the gate decorative.
- **`GITHUB_TOKEN`, not a personal access token.** GHCR accepts the token the workflow already has,
  scoped with `packages: write` on the one job that needs it. Nothing to create, rotate or leak.
- **`concurrency` cancels superseded CI runs but never a CD run.** Interrupting a publish leaves a
  half-written manifest in the registry.
- **Images are built for amd64 and arm64.** Runners are amd64; an Apple Silicon laptop pulling an
  amd64-only image runs it under emulation, slowly.

## Trade-offs accepted

- **Vendor lock-in to the syntax.** Moving to another provider means rewriting the YAML. The commands
  inside the steps are ordinary `dotnet` and `npm`, so the work is mechanical.
- **Public-repository minutes are free; private ones are metered.** The `concurrency` cancellation and
  the layer caching exist partly for that reason.
- **Multi-architecture builds are slower**, because arm64 is emulated on an amd64 runner. Acceptable
  on a tag, which is why it is not done on every push.

## Related

`docs/GITHUB-ACTIONS-SETUP-PRIVATE.md` is the long-form walkthrough of these files — it is
`.gitignore`d and never published.
