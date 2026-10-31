# Slice 42 — T2 Backend tests build one host per fixture, and a guard keeps it that way

Epic #6264 · ADO #6460 · `@infrastructure` · job_id: infrastructure-only · **Size L** (split per harness group at DESIGN)

## Goal

Backend integration tests build one app host per fixture, not one per test, and each test's database is cheap. A
guard stops a new per-test host from landing.

## Why

- 95 test files build a whole `TestWebApplicationFactory` host (often plus `WithWebHostBuilder`) in `[SetUp]` or
  in the test body. Each test therefore boots the app, recreates its database and runs every seeder.
  `BlockedItemsAcceptanceTest` is a representative example.
- Story #6302 moved the Refinement and Reports harnesses to one host per fixture in mid-October (its DS-Q2). Nothing
  guards the pattern, though: 20 new files did it again in October, 12 of them after that fix
  (`ConnectionDefaultsAcceptanceTest`, `NamedDependencyLinkAcceptanceTest`, `FastWithManyFeaturesAcceptanceTest`,
  the `…OnPostgresTests` added on 2026-10-26, …).
- The backend CI test step went from about 7 min (mid-September) to 9–14 min (late October). The number of backend
  tests went from 8,257 to 11,632 in October alone, and the number of files that build an app host went from 18 in
  March to 163 now.

## IN

- **Measure first** with `FixtureSetupTimer` (`LIGHTHOUSE_FIXTURE_TIMING=1`): set-up against body, per harness.
- **Host per fixture** for the set-up-bound harnesses: `[OneTimeSetUp]` / `[OneTimeTearDown]`, with per-test inputs
  (licence answer, clock, counters) reset in `[SetUp]` through objects the fixture owns, never through a DI
  registration that captures a per-test field. This is the same rule as #6302 DS-Q2. No new `[NonParallelizable]`.
- **Cheap per-test database**: create and seed a template SQLite file once per host, then copy the file per test,
  instead of `EnsureCreated` plus every seeder. This is #6302's owed follow-up.
- **Guard**: an architecture test with a ratcheting allow-list of file names (S05a / FE1 style). It fails when a
  host is built in `[SetUp]` or a test body outside the list, and fails on a stale entry.
- **Small #6302 leftovers**:
  - `ClusterSubstrateProbeRegistrationTest` boots the whole app against a Redis address where nothing listens
    (CI 0.8–16.4 s). Build only the part it inspects, as `DataProtectionKeyRingTest`'s Redis case does.
  - In `ReconstructOverTimeHistoryAcceptanceTest`, the held pass blocks a thread-pool thread on a synchronous wait
    and can hit its 60 s guard on a busy run. Give it its own thread, as `CacheTest` now does.

## OUT

- The memory leak itself (T1). Worker caps (T3). Budget enforcement (T4).

## Acceptance criteria

- [ ] Set-up share recorded per harness before any change.
- [ ] Allow-list guard in place, failing on a newcomer and on a stale entry; the list only shrinks.
- [ ] Summed CI time of the moved classes measured before and after (median of at least 5 runs on main).
- [ ] Parallel safety: 3 consecutive green full-assembly runs; `BackendTestParallelizationGuardTest` green.
- [ ] No test deleted, skipped or weakened; mutation score of touched test helpers held.
