# ADR-074 — Backend test isolation via per-fixture WebApplicationFactory ownership

**Status**: Accepted (DESIGN, 2026-06-15). Final wall-clock numbers validated by the `backend-test-speed` Slice-01 spike.
**Feature**: `backend-test-speed` (ADO #5258). Follows #5020 CS-P.
**Deciders**: solution-architect (DESIGN, propose mode) + user.

## Context

#5020's CS-P enabled `[assembly: Parallelizable(ParallelScope.Fixtures)]` (`Lighthouse.Backend.Tests/GlobalUsings.cs:3`) but allowed `[NonParallelizable]` as the safety valve. Those opt-outs grew to **54 files**, re-serializing the suite; the CI backend test step is back to 12+ min.

Root cause (verified 2026-06-15), one design flaw behind the ~37-fixture majority:

- `IntegrationTestBase` is `[NonParallelizable]` **at the base** (`IntegrationTestBase.cs:8`).
- Its default constructor routes through a single `static Lazy<TestWebApplicationFactory<Program>> SharedFactoryLazy` (line 11) — **one factory, one database, shared by every fixture**.
- `[SetUp]`/`[TearDown]` do `EnsureDeleted()` + `EnsureCreated()` on that shared DB (lines 77-86) — so two parallel tests would wipe each other's data. The base **must** be serial today.

Two facts make the fix light:
- `TestWebApplicationFactory` **already** picks a unique random SQLite file per instance (`IntegrationTests_{random}.db`) and strips `IHostedService`s. Each factory instance is therefore already DB-isolated.
- `IntegrationTestBase` **already** has an `ownsFactory: true` constructor path — per-fixture ownership is a supported, existing seam.

## Decision

Adopt **Strategy A — per-fixture `WebApplicationFactory` ownership**:

1. The `IntegrationTestBase` default constructor creates its **own** `TestWebApplicationFactory` (built once per fixture, its own unique SQLite file) instead of using the static `SharedFactoryLazy`. Remove `SharedFactoryLazy` and the base-level `[NonParallelizable]`.
2. The per-test `EnsureDeleted`/`EnsureCreated` reset stays — it now resets the fixture's **own** DB, and test methods within a fixture run serially under `ParallelScope.Fixtures`, so the reset is collision-free.
3. The factory is disposed + its file deleted in `OneTimeTearDown` (already the `ownsFactory` behaviour).
4. The `Services/Implementation` cluster (~10 fixtures serial for shared Moq / in-memory-DB / dispatcher state) is isolated per-test (fresh mocks in `[SetUp]`, scoped DB/dispatcher) and un-tagged.
5. The inherently-serial residue (`API/Security/**` rate-limiting/CORS-env/API-key-scopes/group-snapshot + `LighthouseAppContextConcurrencyTest`) keeps `[NonParallelizable]` on a **justified allowlist**, enforced by an ArchUnitNET guard that fails the build on any off-allowlist opt-out.

Fixtures then parallelize under the existing assembly attribute; tests within a fixture remain serial (the natural grain — one factory built once per fixture, reused across its methods).

## Alternatives considered

- **Strategy B — shared WAF + per-test DB identity threaded through DI.** Rejected: requires per-scope connection-string plumbing (AsyncLocal/scoped provider) AND a shared WAF still shares in-process singletons (caches, etc.) across parallel tests — a contention/bleed hazard. Higher complexity and isolation risk for no host-build saving that a factory pool couldn't also provide.
- **Status quo (keep adding `[NonParallelizable]`).** Rejected — that *is* the accumulated debt this feature pays down.

## Consequences

- **Cost**: N host builds instead of one (one per fixture). The Slice-01 spike measures per-fixture WAF construction cost (reusing `FixtureSetupTimer`). Mitigations if the parallel wall-clock doesn't beat the serial baseline: bound fixture parallelism (`LevelOfParallelism`), pool a small number of factories, or switch the per-fixture DB to SQLite in-memory (`Mode=Memory`) to cut file I/O. Hosted services are already stripped, so a build is host-graph + EF only.
- **Isolation**: each fixture gets its own singleton graph → strictly cleaner than the shared-WAF status quo. The CS-P `AuthenticationMethodSchema` per-host-singleton precedent (`docs/ci-learnings.md`) continues to hold and is no longer load-bearing for cross-fixture safety.
- **Behaviour preserved**: same fixtures, same test names, same assertions — only factory/DB lifetime changes. No coverage change.
- **Regression-proof**: the ArchUnit allowlist guard stops the 54-opt-out drift from recurring silently (its root cause was the absence of any guard after CS-P).
- **Cross-cutting**: RBAC / Lighthouse-Clients / Website — **N/A** (test-infra only; no authorization path, API contract, or marketed surface changes). Mutation kill rate ≥ 80 % re-validated on any production file touched for an isolation seam.
