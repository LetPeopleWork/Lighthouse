# Feature: story-6460-t2-backend-host-per-fixture

Story #6460 (Active) · T2 Backend tests build one host per fixture, and a guard keeps it that way · part of Epic #6264
([workspace](../epic-6264-modular-monolith/feature-delta.md)) ·
brief: [`slice-42-t2-backend-host-per-fixture.md`](../epic-6264-modular-monolith/slices/slice-42-t2-backend-host-per-fixture.md)

DISCUSS: covered at Epic level (D-6264-3); waves start at DESIGN. This file holds PICK-UP, the measurement and DESIGN
(propose mode, maintainer AFK: the recommended option was taken for every engineering choice and is recorded).
Raw material: `measurement/` (timing CSV and run summary gzipped or small; the scripts that produced every number are in
`measurement/scripts/`, their input paths point at the scratch locations of the 2026-11-01 session).

## Wave: PICK-UP / [REF] Inherited

From the Epic's `feature-delta.md`, by heading. Linked, not copied:

- [REF] How to pick up a Story: the recurring per-Story checklist (copied below to tick).
- [REF] Stories → Test-infrastructure track → T2.
- [REF] Story Map → Pick-up groups → G0b (order T3 → T1 → T2 with FE7 → T4); graph `T1 --> T2` (soft).
  **T1 is delivered and pushed (`b2b516258`)**, so the soft dependency is met. What T2 inherits from T1
  ([story 6459](../story-6459-t1-backend-test-memory-flat/feature-delta.md), DELIVER results):
  - OAuth providers ask `IHttpClientFactory` per request; disposed test hosts are freed again.
  - Test hosts default to `RateLimits:Enabled=false`; a fixture that tests rate limiting opts back in. A host that is
    built per test and disposed now costs time only, not memory. The full run went 19.0 → 4.3 GB peak, 434 → 407 s, at 6 workers.
  - `TestHostIsReleasedAfterDisposeTest` is a `[NonParallelizable]` guard fixture on the allow-list of
    `BackendTestParallelizationGuardTest`. T2 must not add to that list.
- [REF] Cross-Cutting Impact Checklist (answered below). [REF] Outcome KPIs + guardrails: KPI row 16 (CI test step
  minutes on main, median of at least 5 runs) is the one T2 moves; guardrail: every backend test stays green, none deleted,
  skipped or weakened, CI wall time not worse.
- [REF] Story #6302 (`../story-6302-ci-test-time/feature-delta.md`): DS-Q2 (LOCKED, option A: host per fixture in
  `[OneTimeSetUp]`, per-test inputs reset in `[SetUp]` through fixture-owned objects, never a DI registration that
  captures a per-test field; no new `[NonParallelizable]`; 3 consecutive green full runs) and its owed follow-up
  ("copy a seeded template database file instead of migrating and seeding for every scenario"). Rejected there and
  re-weighed here: B "reusable host as the default base" and C "keep a host per test and cut only database cost".
- [REF] `docs/ci-learnings.md` backend, Tests: "Host lifecycles in `WebApplicationFactory` tests" (resolve every service
  before `StopApplication()`; a shared host holds per-test state only in objects the fixture resets in `[SetUp]`).
  That entry says "not automated"; this Story automates the host-per-test half (see D5) and must edit the entry.
- Maintainer decisions for this item (2026-11-01, AFK entry for the test track, recorded in the T1 workspace): rigor
  `standard`; refactor and review per slice, mutation once at the end; nothing is pushed.

## Wave: PICK-UP / [REF] DoR re-run

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | **126 types** start a host for every test (the guard's reconciled count; the PICK-UP classifier saw 80 files, 63 `[SetUp]` + 18 test body - 1 in both; the other 46 reach it through a helper, base class, `*TestHost` or constructor; see "Gate revision 1"); measured set-up is 74.5 s of 170.5 s in 14 timed fixtures that do it. CI backend test step median 746 s. |
| 2 Persona | PASS | `lighthouse-maintainer` waiting on CI; parallel local sessions. |
| 3 Domain examples | PASS | `BlockedItemsAcceptanceTest`, `ConnectionDefaultsAcceptanceTest` (4 of the 5 most set-up-bound timed fixtures), `ClusterSubstrateProbeRegistrationTest`, `ReconstructOverTimeHistoryAcceptanceTest`. |
| 4 UAT G/W/T | DEFERRED | Epic catalogue rule: written at each Story's DISTILL. |
| 5 AC derived from UAT | PASS | The brief's five AC. |
| 6 Right-sized | PASS | Size L; split into five slices below; days per slice are in the "Slice sizes (D1 to D4)" table of Gate revision 1. |
| 7 Technical notes + cross-cutting | PASS | Below. |
| 8 Dependencies | PASS | T1 delivered. No hard dependency left. |
| 9 Outcome measurable | PASS | Summed CI time of the moved classes (per-test CSV artifact `test-timings-backend`, 7 runs saved as the baseline) and the backend test step median. |

Slice composition: waived at Epic level (D-6264-5). JTBD: infrastructure-only.

## Wave: PICK-UP / [REF] Baseline re-count (2026-11-01, `main` at `b2b516258`)

All commands run from `Lighthouse.Backend/Lighthouse.Backend.Tests` (`git grep`, so only tracked files). The classifier
(`measurement/scripts/classify.py`, output `measurement/host-construction-classification.txt`) attributes every host
construction to the enclosing member by indentation and the NUnit attributes above it. It is a heuristic: a call inside
an un-attributed helper is reported as `helper` and only the guard's IL scan (D5) settles who calls that helper.

| Brief says | Command | Today |
|---|---|---|
| 95 files build a `TestWebApplicationFactory` host in `[SetUp]` or a test body | `git grep -l 'new TestWebApplicationFactory' -- '*.cs'` = **111** files, **138** occurrences. `git grep -l 'WithWebHostBuilder' -- '*.cs'` = 102 files, 107 lines. Classified (`python3 -I classify.py`, `summ2.py`): `new` in `[SetUp]` or a test body = **71** files; derived hosts (`WithWebHostBuilder` / `WithTestAuthentication`) in `[SetUp]` or a test body = 67 files; either = **80** files (63 `[SetUp]`-only, 18 with a test body). | **80** (brief: 95; the 15 fewer are most likely T1 and #6302 moves plus the brief counting helper-only files; the brief's 95 and the other counts are laid side by side in "Reconciliation of counts (A7)" under Gate revision 1) |
| Built per fixture | the same classifier, `[OneTimeSetUp]` | **18** files |
| Built in a helper (settled only by the IL scan) | classifier kind `helper` | **27** files that have no `[SetUp]` or test-body construction. Most are the #6302 harness bases (Refinement, Reports, BulkReports) whose helper runs from `[OneTimeSetUp]`; some are per-test (`NamedDependencyLinkAcceptanceTest`, the TaskManager `Slice0x` specifications) |
| Built in a constructor, class-level or unresolved | classifier kinds `ctor`, `class-level`, `unknown` | 25 files with nothing else (the `*TestHost` helpers, `IntegrationTestBase`) |
| Files that build any app host | `git grep -lE 'new TestWebApplicationFactory\|new .*WebApplicationFactory\|: IntegrationTestBase' -- '*.cs'` | **169** (brief: 163 files). 132 of them are classified above; the rest only derive from a base |
| `IntegrationTestBase` subclasses | `git grep -lE ': IntegrationTestBase\b' -- '*.cs'` | **56** files. NUnit makes one instance per fixture and the base builds its host in the constructor, so these already have **one host per fixture**; what they repeat per test is the database (`EnsureCreated`, then delete) |
| `[FixtureLifeCycle(InstancePerTestCase)]` anywhere | `git grep -c InstancePerTestCase -- '*.cs'` | none, so a constructor or field initialiser is once per fixture |
| Backend tests | run below | 11,901 (11,898 passed, 2 failed, 1 skipped) |
| Named examples | `git ls-files \| grep <name>` | all six exist: `BlockedItemsAcceptanceTest` (`[SetUp]`, untimed), `ConnectionDefaultsAcceptanceTest` (`[SetUp]`, timed), `NamedDependencyLinkAcceptanceTest` (helper called per test), `FastWithManyFeaturesAcceptanceTest` (`[SetUp]`, untimed), `ClusterSubstrateProbeRegistrationTest` (test body, builds the whole app against Redis `localhost:6399`), `ReconstructOverTimeHistoryAcceptanceTest` (held pass `Task.Run` at line 693, sync waits at 995/1015/1062/1130 with the 60 s guard at 67). The `…OnPostgres` classes are under `Integration/Containers/` (Docker, own container guard): **out of scope** |

Stated limit of the count: the classifier's "per-test" is certain only where the construction sits directly in a
`[SetUp]` or a test method. The 27 helper-only files are resolved in slice 1 when the guard's first run produces the exact list.

## Wave: PICK-UP / [REF] Cross-cutting re-check

- RBAC: N/A, because no authorization rule changes; only how test hosts are built.
- Lighthouse-Clients: N/A, because no route, DTO or serialized response changes.
- Website: N/A, because nothing user-visible changes.
- Usage-data event: N/A, because behaviour-free test infrastructure; nothing a user does is counted. DEVOPS confirms.
- UI sketch (and the loading/failure look): N/A, because nothing a user sees changes.
- Terminology: N/A, because no user-facing words change.
- ADR-270 / ARCHITECTURE.md: see the section below (no change).

## Wave: PICK-UP / Checklist

- [ ] Behaviour-free: every test stays green, none deleted, skipped or weakened (E-names before/after, E-weaken on the diff, as in #6302).
- [ ] Ratchet in the same change: the guard lands in slice 1 and only shrinks afterwards.
- [ ] Moves separate from edits: converting a fixture is an edit; a pure move of a shared helper is its own commit.
- [x] Re-count before designing (above).
- [ ] Mutation: see D12 (test code only; Stryker.NET mutates the product assembly, to be confirmed in DEVOPS).
- [ ] Architecture docs in the same change: N/A for ADR-270 / ARCHITECTURE.md; `docs/ci-learnings.md` backend entry edited in slice 1.
- [ ] Payoff proved: summed CI time of the moved classes and the step median, before (saved) and after (post-push, lead).
- [ ] No user-facing release notes.
- [x] Cross-cutting re-check answered (above).

## Wave: DESIGN / [REF] Measurement (2026-11-01, one standard-filter run)

Machine quiet, 12 cores, `dotnet` 10, `LIGHTHOUSE_TEST_WORKERS=6 LIGHTHOUSE_FIXTURE_TIMING=1`, standard filter, `main` at
`b2b516258` (build clean, 0 warnings). **11,898 passed, 2 failed (the two known publish-output scans), 1 skipped;
6 m 38 s test time, 402 s wall.** Output: `measurement/fixture-setup-timings.2026-11-01.csv.gz`, `run-summary.txt`.

### What the timer covers, and what it does not

`FixtureSetupTimer` is called from `IntegrationTestBase` and five harness bases only: **109 fixtures, 1,047 tests of 11,901**.
Its `SetUp` figure is "the harness's whole `[SetUp]`": host start (when per test), `UseFreshDatabase`, `EnsureCreated`,
seeding, clock and licence reset. The ~70 files that build a host in their own `[SetUp]` are not timed at all (Finding F1).

### Set-up against body, by group (timed fixtures)

| Group | Fixtures | Tests | Set-up total | Mean per test | Reading |
|---|---|---|---|---|---|
| A. Host per test (no `[OneTimeSetUp]`, mean >= 250 ms) | 14 | 182 | **74.5 s** | 409 ms | host build + database + seed every test |
| B. Harness host per fixture (`[OneTimeSetUp]` > 0), database per test | 38 | 488 | **74.8 s** | 153 ms | the #6302 result: the host is gone, the database create + seed is what is left |
| C. `IntegrationTestBase` style (host in the constructor), database per test | 57 | 377 | **21.3 s** | 56 ms | database create (`EnsureCreated`) is the whole cost |
| Total timed | 109 | 1,047 | 170.5 s (teardown 9.0 s, one-time set-up 3.6 s) | | |

Body time is not recorded by the timer. Reference points from CI (below): the cheapest test of a group-A fixture takes
about 1.9 times its local set-up mean (CI min 798 ms against 411 ms local), which fits set-up being the dominant share
of those tests.

### Top 10 set-up-bound timed fixtures (total set-up seconds, local, 6 workers)

| # | Fixture | Tests | Set-up total | Mean | Teardown | Group |
|---|---|---|---|---|---|---|
| 1 | `Slice05bReferencedFieldsAndOlderPayloadsScenarios` | 32 | 15.5 s | 486 ms | 0.3 s | A |
| 2 | `Slice01TeamFeatureSizeTest` | 32 | 11.0 s | 343 ms | 0.2 s | A |
| 3 | `Slice05aTeamsParentOverrideDefaultScenarios` | 18 | 9.9 s | 549 ms | 0.2 s | A |
| 4 | `Slice12EstimationDefaultScenarios` | 20 | 8.7 s | 437 ms | 0.2 s | A |
| 5 | `Slice13ReadinessTest` | 44 | 6.6 s | 151 ms | 0.3 s | B |
| 6 | `Slice01SavesThatStartNothingTest` | 15 | 4.8 s | 317 ms | 0.1 s | A |
| 7 | `Slice01TeamReportScenarios` | 30 | 4.7 s | 157 ms | 0.2 s | B |
| 8 | `Slice04RefinementCadenceTest` | 31 | 4.4 s | 143 ms | 0.2 s | B |
| 9 | `Slice11CastAVoteTest` | 29 | 4.4 s | 151 ms | 0.2 s | B |
| 10 | `Slice03StageRulesTest` | 30 | 4.4 s | 146 ms | 0.2 s | B |

Next in group A: `Slice01FieldChangeUpdatesItsOwnersTest` 4.3 s (teardown 2.4 s, the largest teardown), `Slice08PortfoliosParentOverrideDefaultScenarios`
4.2 s. By harness family the group A total of 74.5 s is: `ConnectionDefaultsAcceptanceTest` family **47.6 s** (8 classes),
`TeamFeatureSizeAcceptanceTest` 16.5 s, `ConnectionFieldChangeAcceptanceTest` 10.5 s.

### Set-up-bound fixtures the timer cannot see (CI proxy)

For the 71 files that build a host in their own `[SetUp]` and are not timed, the cheapest test in a class in CI is a
lower-bound of body-free work and an upper bound of per-test overhead. `n x CI-min` per file (7 CI runs, summed CI
seconds, `measurement/per-test-host-files-with-ci-overhead-proxy.txt`):

| File (classes under it) | Tests | Overhead proxy | Summed CI time |
|---|---|---|---|
| `TaskManager/TaskManagerAcceptanceTest` (24 classes) | 178 | 122 s | 191 s |
| `ConfigureOncePerConnection/ConnectionDefaultsAcceptanceTest` (timed) | 98 | 82 s | 124 s |
| `UsageData/UsageDataCollectorObservationTest` (14) | 279 | 80 s | 104 s |
| `PercentilesOverTime/ReconstructOverTimeHistoryAcceptanceTest` (7) | 54 | 53 s | 239 s |
| `FasterUpdates/FasterUpdatesAcceptanceTest` (7) | 94 | 50 s | 85 s |
| `FastWithManyFeatures/FastWithManyFeaturesAcceptanceTest` (5) | 40 | 37 s | 51 s |
| `ForecastRealityCheck/ForecastRealityCheckAcceptanceTest` (3) | 81 | 33 s | 51 s |
| `DeliverySources/DeliverySourcesAcceptanceTest` (5) | 52 | 29 s | 44 s |
| `BlockedItems/BlockedItemsAcceptanceTest` (6) + `PortfolioBlockedHistoryAcceptanceTest` (5) | 71 | 41 s | 49 s |
| `BehaviourSettings/BehaviourSettingsAcceptanceTest` (4) | 50 | 22 s | 44 s |
| `ManualSorting`, `PercentilesOverTime`, `TeamFeatureSize`, `ConnectionFieldChange`, `Dependencies` | 237 | 98 s | 167 s |
| remaining ~55 small files (each under 12 s of proxy) | ~590 | ~120 s | |
| **All 76 per-test files (Docker/Postgres classes excluded)** | **1,682** | **791 s** | **1,348 s** |

Untimed share: 71 files, 1,496 tests, proxy 669 CI-seconds. The proxy over-states the saving where a class has no cheap
test (`ReconstructOverTimeHistory`: its min test is a real scenario, so its 53 s is a ceiling) and under-states it where
the minimum is itself a slow test; it is a ranking, not a forecast. Finding F1's remedy (D1) makes it measurable.

### Reachable saving (estimate, summed seconds; not a promise)

Three separate levers, each stated against its own evidence:

1. **Host per fixture for the per-test-host files.** Timed group A: 74.5 s -> about 28 s (182 tests x group B's 153 ms) =
   **-46 s local, about -83 s CI-summed**. Untimed 71 files: the move leaves the database cost (about 40 % of a group A
   set-up), so about 60 % of the 669 s proxy = **about -400 s CI-summed**.
2. **Template database** (D4). Cost it removes: the 56 ms of group C is pure `EnsureCreated`; group B's extra ~100 ms is seeding.
   Schema-only template: up to ~50 ms x about 2,500 database-per-test tests (the 1,047 timed + the moved ones) = **-125 s local,
   about -150 s CI-summed**. Template with seeders applied: up to ~-380 s CI-summed. Both are ceilings until the one-fixture A/B in slice 2 measures them.
3. **Leftovers** (D8, D9): `ClusterSubstrateProbeRegistrationTest` 0.8-16.4 s in CI; the held pass is a flake cure, not a saving.

**Total: about 620-850 CI-summed seconds of the 2,806 s summed test time (22-30 %).** CI runs about 3.8 tests in parallel
(2,806 s summed / 746 s step), so the **step median could fall by roughly 2.5-3.5 minutes (746 s -> about 530-600 s)**. Treat it
as the size of the prize that justifies the Story, not as the AC; the AC is the measured before/after in the next section.

### CI baseline (read-only, nothing pushed)

- **Backend test step** (`Test Backend (with Coverage)` in `ci.yml` -> `Verify Backend / backend`, from `gh run view --json jobs`),
  23 successful `main` runs 2026-10-29 .. 2026-11-01: **median 746 s (12.4 min)**, range 559-893 s, bimodal (about 560-720 s and
  830-890 s; not explained by commit, likely runner variance). Per-run values: `measurement/ci-backend-test-step-seconds.tsv`.
  (`gh run list --workflow ci_backend.yml` returns nothing: it is a reusable workflow; the runs are listed under `ci.yml`.)
- **Per-test CI durations are obtainable**: the `test-timings-backend` artifact (`test-timings-backend.csv`, ~2 MB, one row per test)
  is published by every run. I downloaded the 7 most recent successful runs (`41334722014 41274180320 41228644313 41227946204 41216642436 41211823258`
  plus `41293660335` for the format check) and computed the per-class median of summed seconds
  (`measurement/ci-class-median-7-runs.tsv.gz`; 943 classes). Summed test time per run: 2,139-2,937 s, **median 2,806 s**.
  These are the **"before" baseline** for the brief's AC "summed CI time of the moved classes, median of at least 5 runs".
- **What cannot be had before a push**: the "after" side. It needs at least 5 `main` runs containing each slice's commit, so the
  AC's after-measurement is a post-push step **owned by the lead**; the same two commands reproduce it (list runs, download the
  artifact, `cistats.py`).
- Caveat on the baseline: the saved runs predate the T1 push, and T1 changed host disposal (memory), not time; the CI numbers
  include coverage instrumentation, so they are about 1.7-2x the local ones (calibrated on 12 timed group A fixtures).

## Wave: DESIGN / [REF] Decisions (propose mode, maintainer AFK, 2026-11-01)

Shape of the work: test infrastructure only (one shared test factory, one new guard test, fixtures converted to the existing
`[OneTimeSetUp]` pattern). Nothing a user sees changes. Recommended option taken for every choice.

| # | Decision | Chosen | Why / trade-off |
|---|---|---|---|
| D1 | Make the untimed fixtures measurable | `TestWebApplicationFactory` gets a `CreateHost` override that reports host-build time to `FixtureSetupTimer` (new `MeasurementKind.HostBuild`, attributed to the running fixture via `TestContext.CurrentContext.Test.ClassName`), in slice 1. | The timer covers 1,047 of 11,901 tests; the AC "set-up share per harness before any change" is otherwise unmet for 71 files. One override in the one factory beats touching 71 files. Rejected: instrumenting each file (71 edits for a number we then delete). |
| D2 | Which fixtures move first | Order rule applied (Gate revision 1): the guard first, then measured before proxy, the template's A/B gating its size; "biggest first" is dropped. The ranking below is the starting order, re-sized after slice 1's D1 measurement: (1) the `ConnectionDefaultsAcceptanceTest` family (47.6 s local set-up, 4 of the top 5 timed) + `ConnectionFieldChangeAcceptanceTest` (10.5 s), with the guard; (2) the template database (central, benefits every database-per-test fixture, no per-fixture edit); (3) `TaskManager`, `ReconstructOverTimeHistory`, `FasterUpdates`, `UsageDataCollectorObservation` (largest untimed proxies, 305 s); (4) the mid-size acceptance bases; (5) the long tail. Rule for the tail: a file whose measured host-build share is under 25 % of its class time stays on the allow-list with a reason ("body-bound: ..."). | Ordered by measured seconds before proxy seconds (not "biggest first"; see Gate revision 1). The tail is cheap per file but each move is an edit with a regression risk; below a quarter of the time it is not worth it. |
| D3 | Per-fixture host pattern | Exactly #6302 DS-Q2: host built in `[OneTimeSetUp]`, disposed in `[OneTimeTearDown]`; per-test inputs (licence answer, clock position, counters, captured calls) live in objects the fixture owns and are reset in `[SetUp]`; ~~the DI registration closes over the **fixture-owned object**, never a per-test field.~~ (Struck: the rule is the per-scope one made concrete under Gate revision 1, B1 to B6 below: the factory owns the per-test inputs and the database file name is read per scope.) No new `[NonParallelizable]`. A fixture that keeps a per-test derived host (it flips a configuration) uses one derived host per distinct configuration, created in `[OneTimeSetUp]`, or stays on the list with the reason. | It is the locked, measured pattern with 3 precedents (Refinement, Reports, TeamFeatureSize left alone). Risk it carries, to be tested per slice: state that used to be thrown away with the host (singletons, caches, `FakeLighthouseClock`) now lives across tests; each converted fixture is run alone three times in a row before commit. **Made concrete in Gate revision 1 (B1, B2, B3, B5):** the factory owns one `FakeTimeProvider` field that a converted fixture resets to `TestToday.FrozenNow` in `[SetUp]`; the database file name is read per scope, so `UseFreshDatabase()` is seen only by scopes created after it and a converted fixture never resolves `LighthouseAppContext` or its options from the root provider (recorded grep); rate-limiter partitions are reset by keeping a derived host per configuration or the fixture stays listed; `[OneTimeTearDown]` disposes the host before database files are deleted. |
| D4 | Cheap per-test database | **Process-wide schema template, copied per fresh database, inside `TestWebApplicationFactory`.** A `Lazy` static builds one SQLite file with `Database.EnsureCreated()` (same `UseSqlite(..., DatabaseConfigurator.ConfigureSqlite)` options, `Pooling=False`, WAL checkpointed away and no `-wal`/`-shm` left); the options callback, which `AddDbContext` runs once per scope and which reads `databaseFileName` each time, copies the template to that path the first time a scope sees a path that does not exist yet (built under a temporary unique name and moved atomically to a per-content or per-run name; copied only from a finished file; a unique destination per copy; Gate revision 1, B4), so `UseFreshDatabase()` and the 111 files that call `EnsureCreated` themselves keep working (EnsureCreated on a file that already has tables is a no-op). Slice 2 first proves it on one group-B and one group-C fixture (median of at least 5 runs per fixture at the same worker count; set-up mean must fall by at least 30 % in each group, and the template ships only for the group that passed; Gate revision 1, B6); only then is it the default. Entry condition: scenario DB-4 (two processes build at once) is green. Seeded variant (D4b): a per-host template with the `ISeeder`s already applied, **only if** the A/B shows seeding is what remains; decided by that measurement, not now. | Feasibility checked against today's code: the factory builds a unique file name per factory (`NewDatabaseFileName`), the options lambda reads the field on every options build, `UseFreshDatabase` already swaps the name, only `TestWebApplicationFactory` creates SQLite files (Postgres tests use `SharedPostgresContainer`, **out of scope**), the model is identical for every host, so one process-wide template is valid. Central, one file, benefits ~2,500 tests with no per-fixture edit. Risks: a test that expects a database with no tables at first use (the migration/health fixtures use their own set-up and are checked in slice 2); Windows file locking (template opened read-only for the copy; the existing delete-retry loop stays). Rejected: a per-host template (N builds instead of 1, no gain over process-wide for schema); an in-memory SQLite connection (FK/WAL/file semantics differ from the tests' file databases); EF InMemory (does not enforce FKs, per ci-learnings). |
| D5 | Guard design | `Architecture/TestHostPerFixtureGuardTest`: a ratcheting allow-list, **IL-scan with Mono.Cecil** (the same dependency and style as `IlDependencies`). Roots: methods carrying `[SetUp]`, `[Test]`, `[TestCase]`, `[TestCaseSource]`, `[Theory]` in the test assembly (inherited ones included). Reachable set: the call closure through methods and constructors **declared in the test assembly** (so a helper, a `*TestHost` constructor and a base-class `[SetUp]` are followed). Sinks: `newobj TestWebApplicationFactory<>` / `WebApplicationFactory<>`, and calls to `WithWebHostBuilder` or `WithTestAuthentication`. An offender is reported as the **top-level declaring type of the root method** (a base class `[SetUp]` is one entry for all its subclasses). Two tests, both directions, as in `PostgresContainerSharingGuardTest` and `RepositoryLeakRatchetArchUnitTest`: (a) no offender outside the list: the message names the type and says "build the host in `[OneTimeSetUp]`, reset per-test inputs in `[SetUp]`"; (b) every list entry still offends, else "remove it from the list". The comparison is a small pure step driven with made-up lists, so both failure directions are proven without breaking real code. The list starts as today's exact offenders (126 types, keyed by full type name, with a ceiling test) and may only shrink. | Precedents: `PostgresContainerSharingGuardTest` (source scan, file-name list with reasons, stale-entry test), `BackendTestParallelizationGuardTest` (type-name list), `RepositoryLeakRatchetArchUnitTest` (pure compare step + planted lists). IL beats a source scan here because the pattern is usually in a helper that `[SetUp]` calls, and the scan must follow calls; a regex over files cannot. Key is a type name, not a file name as the brief words it: the IL has no file, and the type is what a reviewer reads. Rejected: source scan with attribute-by-indentation (the classifier above needed three kinds of guess and still left 27 "helper" files unresolved); an NUnit `[SetUp]`-time runtime hook counting factories (catches only what runs, slows every run, cannot fail at build review time). Fallback if the closure is too noisy at DELIVER (a fixture that builds a host in a lambda it hands to a base): fall back to source scan + the Postgres guard's shape, and record why. |
| D6 | Guard sits where | In the standard `dotnet test` run, `Architecture/` folder, reads the already-built test assembly (`typeof(...).Assembly.Location`, as `IlDependencies.ReadModule` does); no CI workflow change. | Same place as the other allow-list guards; CI already runs it. |
| D7 | Existing instrumentation | The `FixtureSetupTimer` env switch stays; D1 only adds one kind. The `check_budget.py` backend step (`--warn-only` today) is untouched; its per-class budgets will start showing the moved classes' gains. | Reuse. |
| D8 | #6302 leftover: `ClusterSubstrateProbeRegistrationTest` | Build only the part it inspects. `Program.ConfigureHealthChecks(WebApplicationBuilder)` (Program.cs line 1042, `private static`) is what registers `cluster-substrate` with `StartupOnlyTags`; widen it to `internal static` (a one-word, behaviour-free change, the same step `ConfigureDataProtection` already took), then the test builds `WebApplication.CreateEmptyBuilder`, sets `ConnectionStrings:Redis` to `localhost:6399`, calls it, and reads `HealthCheckServiceOptions.Registrations` (checks are only registered, never resolved, so nothing connects). Same two assertions (startup tag yes, ready tag no) plus the registration-exists one. Slice 1. | Removes a 0.8-16.4 s CI test that boots the whole app against an address where nothing listens. **Cost: a `Program.cs` edit**, which per the project's memory forces the full backend Integration suite (live connector tests) before push. Accepted because the edit is a visibility change; if the maintainer prefers not to pay that run, the fallback is to leave the test as is and keep it on the guard's list with the reason "boots the app to inspect a registration". |
| D9 | #6302 leftover: held pass thread | In `ReconstructOverTimeHistoryAcceptanceTest`, run the held pass on its own thread: `Task.Factory.StartNew(..., TaskCreationOptions.LongRunning).Unwrap()` instead of `Task.Run` at lines 693 and 732 (and the replica pair at 781-782), exactly as `CacheTest` does (line 21). The 60 s guard (`LongEnoughThatSomethingIsWrong`) stays. Slice 3, with that class's host move. | Matches the ci-learnings rule "blocking waits park the thread pool"; verify with `DOTNET_PROCESSOR_COUNT=4` beside another class, as the rule says. |
| D10 | Postgres-backed tests | Out of scope. | They already share one container (`SharedPostgresContainer`, `PostgresContainerSharingGuardTest`); their cost is a server, not a host. They also run in `Integration/Containers` and are mostly category-excluded locally. |
| D11 | Behaviour-free proof | Per slice: (1) E-names: the sorted list of test ids before and after, equal (`dotnet test --list-tests`), as #6302 did; (2) E-weaken on the diff: no `[Ignore]`, `[Explicit]`, `Assert.Ignore`, `NonParallelizable` added, no `Assert.` line removed (grep over the slice diff); (3) parallel safety: **3 consecutive green full standard-filter runs at the end** (not per slice), `BackendTestParallelizationGuardTest` green and unchanged; (4) each converted fixture run alone 3 times in a row. | The Story's AC verbatim. |
| D12 | Mutation | **N/A for test code, to be confirmed at DEVOPS**: Stryker.NET mutates the product assembly and the Story edits only the test project. The guard proves itself through its planted made-up lists (both failure directions); the template-copy code is proven by the A/B and by 3 green full runs. If DEVOPS finds the Stryker config does mutate the test helpers, run it once at the end on `TestWebApplicationFactory.cs` and the guard. | The AC "mutation score of touched test helpers held" has nothing to hold if the tool does not see them; checked, not assumed, at DEVOPS. |
| D13 | Rigor profile (proposal) | `standard` for all five slices (crafter/designer Sonnet, reviewers Haiku), as the AFK entry for the test track already chose; `thorough` for slice 2 (template database, central, biggest blast radius) if the maintainer prefers. | Mechanical conversions follow a locked pattern; the template is the one design-heavy step. |

## Wave: DESIGN / [REF] Hexagonal sketch

Test infrastructure only; the product hexagon is untouched. The "ports" of the test side:

```
                       NUnit fixture (driving side of the test)
                                   |
          [OneTimeSetUp] -----> TestWebApplicationFactory<Program> --- CreateHost (NEW: timed, D1)
          once per fixture              |  options lambda reads databaseFileName
                                        |        |
   [SetUp] per test:                    |        +--> TemplateDatabase (NEW, process-wide, built once, D4)
   fixture-owned objects reset          |                 |  File.Copy per fresh file
   (clock, licence, counters)           v                 v
          \                   real ASP.NET host ---> SQLite file (copied from the template, then dropped at teardown)
           \---------------------------> (unchanged product code; one visibility change in Program.cs, D8)

   TestHostPerFixtureGuardTest (NEW) --reads--> compiled test assembly (IL) --> allow-list (shrinks)
```

New: `TemplateDatabase` (inside `TestWebApplicationFactory.cs`), `HostBuild` timing kind, `TestHostPerFixtureGuardTest`. Modified: about
80 fixtures/bases (host to `[OneTimeSetUp]`), `TestWebApplicationFactory`, `docs/ci-learnings.md`. Unchanged: all product behaviour (the only product-side edit is `ConfigureHealthChecks` going from `private` to `internal`, D8),
the `ISeeder`s, `IntegrationTestBase`'s contract (its `EnsureCreated` becomes a no-op on a copied file).

## Wave: DESIGN / [REF] Component decomposition

| Component | Location | Change |
|---|---|---|
| Host-build timing | `TestHelpers/FixtureSetupTimer.cs`, `TestHelpers/TestWebApplicationFactory.cs` | EXTEND (D1) |
| Template database | `TestHelpers/TestWebApplicationFactory.cs` (a small nested/internal type) | EXTEND (D4) |
| Guard | `Architecture/TestHostPerFixtureGuardTest.cs` (+ `IlDependencies`-style Cecil walk) | CREATE NEW (the call-closure walk is new; the module reader is reused) |
| Harness bases and fixtures | per slice list below | EXTEND (convert to `[OneTimeSetUp]`) |
| `ClusterSubstrateProbeRegistrationTest` | `Health/` | EXTEND (D8) |
| Held pass | `API/Integration/PercentilesOverTime/ReconstructOverTimeHistoryAcceptanceTest.cs` | EXTEND (D9) |
| Ledger entry | `docs/ci-learnings.md` backend Tests | EXTEND (host lifecycle: say what the guard now automates) |

## Wave: DESIGN / [REF] Reuse Analysis

| Need | Existing thing | Decision |
|---|---|---|
| Host once per fixture | `ReportsAcceptanceTest.StartTheHost` / `RefinementAcceptanceTest` / 11 read-API fixtures (slice 06 of #6302) | EXTEND: copy the shape into each converted base; no shared "default base" (#6302 B rejected, re-weighed below) |
| Fresh file database per test | `UseFreshDatabase`, `NewDatabaseFileName`, `NoWaitingForTheDisk` | EXTEND |
| Ratcheting allow-list | `PostgresContainerSharingGuardTest`, `BackendTestParallelizationGuardTest`, `RepositoryLeakRatchetArchUnitTest` | EXTEND the pattern (both-direction tests, pure compare step) |
| IL reading | `IlDependencies.ReadModule` (Mono.Cecil already referenced) | REUSE; the call-closure walk is CREATE NEW |
| Timing | `FixtureSetupTimer`, `FixtureTimingReporter` | EXTEND |
| Redis registration without the app | `DataProtectionKeyRingTest` Redis case | REUSE |
| Own-thread pass | `CacheTest` `LongRunning` | REUSE |

## Wave: DESIGN / [REF] Rejected alternatives

- **A reusable default host base for every integration fixture now** (#6302 option B). Would touch ~80 fixtures with no
  evidence of which are body-bound, and a hook in `IntegrationTestBase`'s constructor is a virtual call in a constructor
  (Sonar S1699). Re-weighed: still rejected; `IntegrationTestBase` already is one host per fixture for 56 files.
- **Cut only database cost, keep a host per test** (#6302 option C). Measured: group A pays 409 ms per test, of which only about
  half is the database. Template alone would leave ~250 ms x 1,500 tests. Kept as D4, not instead of the moves.
- **Template per host / in-memory SQLite / EF InMemory**: see D4.
- **Runtime counting hook instead of an IL guard**: see D5.
- **Source scan with indentation heuristics for the guard**: see D5.
- **Moving everything in one slice**: 80 fixtures in one commit cannot be reviewed or bisected; five slices, each shipping alone.
- **Simplest alternative first (nothing new at all)**: "just lower `LIGHTHOUSE_TEST_WORKERS`" or a faster runner changes the
  wall time, not the work, and CI keeps its own default. Insufficient: the work is 22-30 % of summed time.

## Wave: DESIGN / [REF] Behaviour-free proof

D11 above. Specifics: the template is a pure optimisation of the set-up (`EnsureCreated` produces the same schema from the same
model; the test `EnsureCreated` calls become no-ops on an existing schema); a fixture converted to `[OneTimeSetUp]` must show
(a) the same test ids (E-names), (b) no assertion removed (E-weaken), (c) passes alone three times in a row (state
leaking across tests would show as a flake here, not only at the end), (d) the final 3 consecutive green full runs. Rollback is
`git revert` of the slice commit; each slice is independent, and the guard's list can be re-extended for a reverted slice in the same revert.

## Wave: DESIGN / [REF] Proposed slicing

Each slice ships alone, is independently valuable, and is ordered by measured saving (biggest first). Slice 1 carries the guard so
the pattern stops growing from the first push. Sizes are rough; the lead confirms them at DISTILL.

| Slice | Content | Why it stands alone | Ceiling, unmeasured (CI-summed; corrected in Gate revision 1, superseding the figures below) |
|---|---|---|---|
| 1. Guard + measure + first move | D5 guard with today's offenders listed; D1 host-build timing; the `ConnectionDefaultsAcceptanceTest` family (8 classes) and `ConnectionFieldChangeAcceptanceTest` moved to a host per fixture; D8 `ClusterSubstrateProbeRegistrationTest`; ci-learnings entry | The pattern cannot grow; the top timed offenders are fixed; the untimed ones become measurable | about -105 s (58 s local x 1.8) + the Cluster test (0.8-16 s) |
| 2. Template database | D4 process-wide schema template in `TestWebApplicationFactory`; A/B on one group-B and one group-C fixture first; optional D4b seeded variant gated on that A/B | One-file change that helps every database-per-test fixture, moved or not | about -150 s (up to -380 s with seeding), measured by the A/B |
| 3. The four biggest untimed fixtures | `TaskManager`, `ReconstructOverTimeHistory` (+ D9), `FasterUpdates`, `UsageDataCollectorObservation` | 305 s of proxy in four files | about -180 s |
| 4. Mid-size acceptance bases | `FastWithManyFeatures`, `ForecastRealityCheck`, `DeliverySources`, `BlockedItems` x2, `BehaviourSettings`, `ManualSorting`, `PercentilesOverTime`, `Dependencies`, `TeamFeatureSize` (its SQL-command counter becomes fixture-owned, the reason #6302 left it) | Next 10 files, 30-40 s of proxy each | about -150 s |
| 5. Long tail | the ~50 small files, each by the 25 % rule; the remaining entries stay on the list with a reason, or the list is empty | Finishes the ratchet; each file is small | about -50 s |

After slice 5 the allow-list holds only files with a written reason, or is gone (then the guard's list becomes a one-line "no host
in `[SetUp]`" rule). The 3-consecutive-run proof and the E-names run after each slice that moves fixtures; the CI before/after is
measured by the lead after each push.

## Wave: DESIGN / [REF] Open questions

1. **Guard key**: the brief says file names; the IL scan reports type names. Chosen: top-level declaring type (D5). If the maintainer wants file names, the source-scan fallback gives them at the cost of the helper-following.
2. **Template content**: schema only (D4) or with the seeders applied (D4b). Settled by the slice 2 A/B, not by opinion.
3. **Windows**: the template copy relies on `File.Copy` and the existing delete-retry loop; the backend suite is not run on Windows in `ci_backend.yml`, so a local Windows run is the only check. Left to the maintainer.
4. **Per-test derived hosts that flip configuration** (rate-limit, auth variants): one derived host per configuration (D3) can multiply hosts per fixture; each such fixture decides at its slice, with the list reason as the fallback.
5. **`check_budget.py` backend step** (`--warn-only`): after slices 1-3 the per-class budgets could be tightened; belongs to T4, not here.

## Wave: DESIGN / [REF] ADR-270 and ARCHITECTURE.md

No change to either: the Story touches no bounded context, adds no domain concept, event or edge, and changes no product module or
layout; it edits only the test project and `docs/ci-learnings.md`. `ARCHITECTURE.md` §4 and ADR-270 stay as they are; `nw-finalize`
should still verify that the only non-test diff under `Lighthouse.Backend/` is that one visibility change (or nothing, if D8's fallback is taken). No new ADR: the pattern is the #6302 DS-Q2 decision plus a ledger entry, as DS-Q2 said.

## Wave: DESIGN / [REF] Usage-data and UI

- Usage-data event: N/A, because no user-facing feature is added; nothing a user does is counted. (Confirmed at DEVOPS.)
- UI sketch and loading/failure look: N/A, because no screen, chart or widget is created or changed.

## Wave: DESIGN / [REF] Patterns weighed (for the maintainer)

- **Chosen: Test fixture setup once (xUnit "shared fixture" / NUnit `[OneTimeSetUp]`)** with per-test reset through owned objects: the fixture, not the DI container, owns the mutable state, so the host is reusable. Rejected: a base class that rebuilds on demand (hidden coupling), a static shared host for all fixtures (state bleed across parallel fixtures, breaks the parallel-safety AC).
- **Chosen: Template/prototype copy** (create the expensive artefact once, copy per test) for the database. Rejected: per-test builder (what we have), shared mutable database with cleanup (parallel unsafe).
- **Chosen: Ratchet / allow-list guard** (fail on newcomer, fail on stale entry), the same as S05a and the Postgres guard: it makes the debt visible and only ever shrinks.
- **Chosen: Open/closed for the guard** via a pure compare step, so the two failure directions are driven with planted data.
- Not applicable here: domain events, outbox, unit of work, value objects, optimistic concurrency (no product code).

## Wave: DESIGN / [REF] Quality gates (this wave)

Documents only; nothing built. Measurement run itself was clean apart from the two known environmental failures
(`NoBuildFileWritesOrReadsTheBackendPublishOutputInsideTheBackendProjectFolder`, `TheScanSeesTheFilesThatConsumeThePublishOutput`).
Peer review of DESIGN: not run here (propose mode); to be dispatched by the lead at the wave gate with `reviewer_model` per the profile.

## Wave: DEVOPS / [REF] Platform decisions (2026-11-01)

AFK mode: the lead took the recommended option on each row; the maintainer reviews before anything is pushed to a shared place. Nothing deploys.

| Decision | Answer |
|---|---|
| Deployment target, orchestration, strategy | N/A, because nothing deploys: no artifact, image or runtime behaviour changes. The one product-side edit (D8, `ConfigureHealthChecks` from `private` to `internal`) has no runtime effect. |
| CI/CD platform | GitHub Actions. `ci.yml` and `ci_backend.yml` were read and are **UNCHANGED**; see the CI/CD section. |
| Branching | Trunk-based on `main`. **One push per slice** (the maintainer allowed pushes for this track), each slice green before it goes (full standard-filter run plus the slice's per-fixture checks), no branches or PRs. Commits stay local until the slice's local checks pass. |
| Rigor | **D13 taken: `standard` for every slice, slice 2 included** (agent model Sonnet, reviewer model Haiku, passed explicitly on every dispatch). The `thorough` option for slice 2 is declined; slice 2 compensates with the one-fixture A/B before the template becomes the default. |
| D8 | **Taken.** `Program.ConfigureHealthChecks` becomes `internal static`; `ClusterSubstrateProbeRegistrationTest` stops booting the whole app. Lands in slice 1. Push-time consequence below. |
| Guard key | **Type name** (the top-level declaring type of the root method), not file name as the brief words it. Why: a type name survives a file move or rename (the guard does not go stale when a class is relocated), and it is what the IL scan sees: IL carries no file name, so a file key would need a second lookup from type to file that can disagree with the compiler. |
| Observability, alerting, SLOs | **N/A, because the Story changes only test code and one visibility keyword; no running service, request path, metric or alert is touched.** The observability this Story needs already exists: `FixtureSetupTimer` (plus D1's `HostBuild` kind) for local timing and the `test-timings-backend` CI artifact for the payoff. The CI "SLO" is the backend test step wall time (median 746 s before); it is measured, not alerted (see the measurement protocol). |
| Usage-data event | **N/A, because it is behaviour-free test infrastructure: nothing a user does changes, so there is no use to count.** No `UsageDataEventName` entry, no line in `docs/settings/usagedata.md`. |
| Security | No secret, permission or scope change. The template database is a temp SQLite file with no data beyond the schema (and, if D4b is taken, the seeders' data, which is demo data). |
| Cross-cutting (RBAC, Lighthouse-Clients, Website, UI sketch with loading/failure look, Terminology, ADR-270) | All N/A as answered in PICK-UP and DESIGN: no route, DTO, screen, word, context or event changes. |
| Rollback | `git revert` of the slice commit. Nothing is persisted; the guard's allow-list is re-extended in the same revert. Each slice is independent (DESIGN, Behaviour-free proof). |

## Wave: DEVOPS / [REF] Environment matrix

Also written as `environments.yaml` in this workspace.

| Environment | Workers | Configuration | Role |
|---|---|---|---|
| Local, quiet machine (12 cores, 31 GB, one suite at a time) | `LIGHTHOUSE_TEST_WORKERS=6` | Debug, standard filter | The per-slice local checks (below) and the local timing (`LIGHTHOUSE_FIXTURE_TIMING=1`). Same setup as the DESIGN measurement, so before and after compare. |
| Local, any session | The Story 6461 cap (half the cores when unset and not CI) | Debug | The guard and the converted fixtures run in the normal backend run. |
| Local, Windows (Standalone and Windows developers) | Cap as above | Debug | Not run by CI. The only place the template copy's file-lock risk can show (see the Windows section). |
| CI (`ubuntu-latest`, 4 cores, `CI=true`) | Each tool's own default (NUnit 4 workers; the 6461 cap is off in CI) | **Release**, `dotnet test -c Release --no-build --no-restore ... -- RunConfiguration.MaxCpuCount=0`; with coverage when `run_sonar`, without otherwise (one step runs per job) | Runs the guard and every converted fixture; the only source of the "after" numbers that count (coverage makes it about 1.7-2x the local time). |

## Wave: DEVOPS / [REF] CI/CD

- **The ci workflows are unchanged** (`.github/workflows/ci.yml`, `ci_backend.yml` read-only checked). Nothing is added, widened or reordered.
- **The guard runs in the normal test step.** `TestHostPerFixtureGuardTest` is an NUnit test in `Lighthouse.Backend.Tests/Architecture/`; both `Test Backend (with Coverage)` and `Test Backend (without Coverage)` run the whole test project, so the guard runs there with no workflow edit (D6). It reads the already-built test assembly from disk, so it needs no extra build output; under `-c Release --no-build` the assembly is the Release one, which is the one the IL scan should see (Release inlining can fold a tiny helper into its caller, and the closure walk follows calls either way, so the offender set can only differ if a helper is inlined into a *different* root type; slice 1 runs the guard once under `-c Release` locally and compares the offender list with the Debug one before the list is committed).
- **Mono.Cecil: already available to the tests, not as a direct reference.** `Lighthouse.Backend.Tests.csproj` has no `PackageReference` to `Mono.Cecil`; it arrives transitively (restore lists `Mono.Cecil 0.11.6` in `obj/project.assets.json`, a dependency of the already-referenced `TngTech.ArchUnitNET`), and `Architecture/IlDependencies.cs` already does `using Mono.Cecil;`. So the guard adds **no package and no csproj edit** and needs no CI change. Consequence worth knowing: Renovate does **not** track Mono.Cecil on its own (it only sees direct references), it moves when `TngTech.ArchUnitNET` moves, which Renovate does cover (`config:recommended` includes the NuGet manager; no `ignoreDeps` for it in `renovate.json`). If DELIVER ever needs to pin Mono.Cecil explicitly, that would be a test-only package change in `Lighthouse.Backend.Tests.csproj`, Renovate would then cover it by default, and it would be recorded in that slice's commit body. Not needed now.
- **Push-time consequence of D8.** Slice 1 edits `Lighthouse.Backend/Program.cs`. Per the repo note (Program.cs edits force the full backend Integration suite) CI then also runs the live Jira, Linear, Azure DevOps and ServiceNow connector tests. State it at push time as a CI consequence, not a defect: that run is longer, and the **Linear key is shared with CI**, so no hand-exploring of the Linear API and no other full local run shortly before the push (rate-limit 429s show up as several unrelated failures, only one naming the 429). Locally, run the connector categories only if a connector changed, which this Story does not. Because the edit is one keyword, slice 1 is otherwise no riskier than the other slices; slices 2-5 touch only the test project and do not widen the CI run. Fallback if the maintainer will not pay that run: leave `ClusterSubstrateProbeRegistrationTest` as is and keep it on the guard's list with the reason "boots the app to inspect a registration" (DESIGN D8).
- **Quality gates per slice before its push:** `dotnet build` (zero warnings; the test project is built with `TreatWarningsAsErrors`), the standard-filter `dotnet test`, the guard green in both directions, `docs/ci-learnings.md` edited in slice 1 and kept true afterwards. No frontend, E2E or Biome gate applies (no frontend or E2E file changes).
- **No CI memory or time gate is added.** `check_budget.py` stays `--warn-only` (D7); tightening it belongs to T4.

## Wave: DEVOPS / [REF] CI "after" measurement protocol (owned by the lead, after each slice's push)

Nothing here can be done before a push; the lead runs it after each slice lands on `main`.

1. **Wait for 7 successful `main` runs** (Gate revision 1, C3; the slice-1 push run is excluded) that contain the slice's commit (list them under `ci.yml`, because `ci_backend.yml` is a reusable workflow and `gh run list --workflow ci_backend.yml` is empty). Include the `main` runs of later slices only if the slice-by-slice attribution is not needed; otherwise stop the count at the next slice's push. The GitHub run-list API serves stale pages about 1 time in 7, so re-list before trusting a count.
2. **Download the artifact `test-timings-backend`** (`test-timings-backend.csv`, one row per test) from each of those runs: `gh run download <run-id> -n test-timings-backend -D <dir>/ci<run-id>`.
3. **Compute the per-class median of summed seconds** with `measurement/scripts/cistats.py <dir>` (it globs `<dir>/ci*/test-timings-backend.csv` and writes `<dir>/ci_class_median.tsv`, one line per class: median seconds, test count).
4. **Compare with the before baseline** in `measurement/`: `ci-class-median-7-runs.tsv.gz` (943 classes, 7 runs, summed test time median 2,806 s) for the moved classes' summed time, and `ci-backend-test-step-seconds.tsv` (23 runs, step median 746 s) for the `Test Backend (with Coverage)` step (read from `gh run view <id> --json jobs`, as the baseline was). Report per slice: summed median seconds of the moved classes before and after, the total summed test time before and after, and the step median before and after. KPI row 16 (CI test step minutes on main, median of at least 5 runs) is the one that moves.
5. **Read it honestly.** The step time is bimodal (about 560-720 s and 830-890 s, runner variance), so compare medians of at least 5 and say so when the ranges overlap. The baseline predates T1's push (T1 changed memory, not time) and includes coverage instrumentation. A slice that does not move its classes is reported as such; nobody edits the baseline.
6. **Where it is recorded:** a short `Wave: DELIVER / [REF] CI after-measurement, slice N` block in this `feature-delta.md` with the run ids, the table and the verdict, plus the new class-median file next to the baseline in `measurement/`.

## Wave: DEVOPS / [REF] Local per-slice checks

Run on the quiet machine at `LIGHTHOUSE_TEST_WORKERS=6`, standard filter (never the live-connector categories).

- **Every converted fixture, run alone 3 times in a row** (`dotnet test --filter "FullyQualifiedName~<Fixture>"`), all green, before the slice is committed. State that leaking from a shared host (singletons, caches, `FakeLighthouseClock`, counters) shows here as a flake, not only at the end.
- **E-names:** the sorted `dotnet test --list-tests` ids before and after are equal (D11).
- **E-weaken on the slice diff:** no `[Ignore]`, `[Explicit]`, `Assert.Ignore` or `NonParallelizable` added, no `Assert.` line removed.
- **Guard:** green in both directions after every slice; the allow-list only shrinks; `BackendTestParallelizationGuardTest` green and unchanged (T1's `TestHostIsReleasedAfterDisposeTest` is the only `[NonParallelizable]` this track relies on and nothing is added).
- **At the end of the track: 3 consecutive green full standard-filter runs** (the Story's AC; not per slice). Two known environmental failures are expected and are not regressions: the two publish-output scans (`NoBuildFileWritesOrReadsTheBackendPublishOutputInsideTheBackendProjectFolder`, `TheScanSeesTheFilesThatConsumeThePublishOutput`); a run with exactly those two and nothing else counts as green for this AC, and the run is repeated, not excused, if anything else fails. In a worktree, copy the gitignored `valid_not_expired_license.json` first.
- **Slice 1 also checks (carried from gate round 2):** a per-test memory trace across one converted fixture (working set sampled between its tests, to see that nothing grows test after test on the now-shared host); re-measure `main`'s peak at 6 workers if the base moved since `b573b1924` before comparing against the 4.73 GB stop threshold; and the commit that removes lines from the list also lowers `MostTypesTheListMayHold` by the same amount (every slice does the same).
- **Slice 2 adds** the one-fixture A/B (one group-B and one group-C fixture, set-up mean must fall by at least 30 %) before the template becomes the default.

## Wave: DEVOPS / [REF] Windows file-lock risk of the template copy (slice 2)

CI is Linux, so CI cannot see this. The Standalone build and Windows developers run the suite on Windows, where an open handle blocks a copy or a delete.

- **Risk:** `File.Copy` of the template while another process or thread has it open for write, or a later delete of a copied database that SQLite still holds open (the existing code already has a delete-retry loop for this).
- **Mitigation 1, unique file names:** every copy gets its own unique name (the existing `NewDatabaseFileName` scheme; the template has a fixed, separate name that no test ever opens as its database). Two tests never share a destination, so there is no destination contention.
- **Mitigation 2, copy instead of open:** the template is built once, behind a `Lazy` static and a lock, with `Pooling=False` and the WAL checkpointed away (no `-wal` or `-shm` file left), then **closed completely before any copy**. Tests only ever `File.Copy` it (read share), never open it with SQLite, so nothing holds a write handle on the source. A copy that fails with an `IOException` is retried through the same bounded retry the delete uses, then fails with a message naming the path (never skipped).
- **Check:** the maintainer or a Windows developer runs the full backend suite once on Windows after slice 1 and again after slice 2 (hold items); this is the only available verification and is listed as a hold item at the end of the track, not claimed as done.

## Wave: DEVOPS / [REF] Mutation strategy

Strategy stays `per-feature` (CLAUDE.md, not edited): Stryker.NET for the backend, kill rate at least 80 %, once at the end on frozen code.

- **D12 confirmed.** `Lighthouse.Backend/stryker-config.json` has `"project": "Lighthouse.Backend.csproj"` (the product assembly) and `"test-projects": ["Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj"]`. Stryker.NET mutates the product project only; the test project is the thing that kills the mutants. Every file this Story edits except one lives in the test project, so **it has no mutants**.
- **D8 is a visibility change with no logic** (`private` to `internal` on a method whose body does not change). Stryker has no mutator for access modifiers, so there is no mutant to kill. The method's body is outside the Story's diff.
- **Test helpers are not mutated by the per-feature config.** If slice 2 adds non-trivial helper logic to `TestWebApplicationFactory` (the template copy, its lock and retry), that code is in the test project, so the per-feature Stryker run never sees it. Its proof is the one-fixture A/B, the three consecutive green full runs and a unit-style test of the helper only if the logic has a branch worth asserting. Recorded here so nobody reads the missing mutation score as an oversight.
- **The guard's pure compare step has its own made-up-list tests instead.** Both failure directions (a newcomer outside the list; a stale entry still listed) are driven with planted lists, in the style of `RepositoryLeakRatchetArchUnitTest`. That is the guard's equivalent of a mutation check: a mutation of the compare would turn one of those tests red. The IL walk is proved on the real assembly (the first run produces the exact offender list) and by one planted-offender case, if the walk can be pointed at a tiny fixture type; otherwise by the real list being exact in both directions.
- **At the end:** no Stryker run is owed. If DELIVER finds the config unexpectedly includes test helpers, run it once at the end on `TestWebApplicationFactory.cs` and the guard, on frozen code, and record the per-file table. Frontend Stryker: N/A, no frontend file changes.

## Wave: DEVOPS / [REF] Coexistence

- **Story 6459 (T1), delivered and pushed (`b2b516258`):** its freed hosts are the reason a host-per-test fixture now costs time only, not memory. T2 does the opposite move (fewer hosts built) and does not undo anything T1 did: OAuth providers still ask `IHttpClientFactory` per request, hosts still default to `RateLimits:Enabled=false`, and a fixture that tests rate limiting still opts back in. A host that now lives for a whole fixture is released at `[OneTimeTearDown]` through the same disposal T1 fixed; the leak guard (`TestHostIsReleasedAfterDisposeTest`, on the serial allow-list) must stay green and the allow-list in `BackendTestParallelizationGuardTest` must not gain an entry from T2. Expected side effect to watch in the "after" runs: peak memory can rise slightly because a fixture's host now lives longer than one test; T1's 4.3 GB peak at 6 workers is the reference, and a clear regression there is a stop signal, not a note.
- **Story 6461 (T3) worker cap:** orthogonal. It sets how many fixtures (so how many hosts) are alive at once; this Story sets how long each lives. Local timings and the three full runs state the worker count (6). CI ignores the cap and keeps its own default.
- **`ci_backend.yml`, `check_budget.py` (`--warn-only`), IDE runners (Rider, Visual Studio), Stryker config:** unchanged; see `environments.yaml`.
- **Shared Postgres container tests** (`Integration/Containers`): out of scope (D10); the template database applies to SQLite files only.

## Wave: DEVOPS / [REF] Risks and carry-forward for DISTILL and DELIVER

1. Shared-host state leaking across tests (singletons, caches, clock): caught by the three-alone runs per fixture and the three full runs; a fixture that cannot be made safe stays on the list with a written reason.
2. Template database: a test that expects an empty database at first use (migration and health fixtures); checked in slice 2's A/B and full runs.
3. Windows file locks on the template copy: unverifiable in CI; a hold item for the maintainer.
4. The Program.cs push in slice 1 runs the live connector suite in CI; announce it, and keep the Linear key quiet beforehand.
5. IL guard noise: if the call closure over-reports at DELIVER, the DESIGN fallback (source scan in the Postgres guard's shape) applies and the reason is recorded.
6. CI step time is bimodal; do not conclude from fewer than 5 runs.
7. No scenario tags in code (project rule): any classification stays in the feature workspace.

## Wave: DISTILL / [REF] Reconciliation

Reconciliation passed -- 0 contradictions. DISCUSS is covered at Epic level. DESIGN and DEVOPS agree on the guard (D5/D6, type-name key,
no csproj or workflow change, Mono.Cecil reached through ArchUnitNET), on the proof (D11) and on the ratchet landing in slice 1.
Maintainer AFK: the recommended option was taken for every choice. UI sketch review: N/A, because nothing a user sees changes.
Two points where DISTILL settled something DESIGN left open, recorded here:

- The guard counts **types, not files** (DESIGN D5). The first run produced **126 types**, not "about 80" (see "Allow-list at the start").
- The planted-fixture cases (DEVOPS: "one planted-offender case, if the walk can be pointed at a tiny fixture type") **are possible**:
  tiny abstract types inside the guard file (ten cases after Gate revision 1), skipped by the real scan. NUnit runs any class that has `[Test]` methods even when it is
  `internal` and has no `[TestFixture]` (measured: the first version ran them), so they are `abstract`.

## Wave: DISTILL / [REF] Scenario list

Classification lives here only; none of it is written into the code (project rule). Test names in the file are plain language.
Test layer: the guard reads the compiled test assembly, so it is a plain unit test (no host, no Docker); the preservation scenarios are the existing tests.

### The guard (written now, live and green, lands in slice 1's commit)

| # | Scenario | Where (`Architecture/TestHostPerFixtureGuardTest`) | Today |
|---|---|---|---|
| G1 | Fails on a newcomer: a type that starts a host in a per-test set-up or test body and is not on the list | `NoTypeStartsAnApplicationHostForEveryTestWithoutBeingOnTheList` | green; RED shown below |
| G2 | Fails on a stale entry: a listed type that no longer does | `EveryTypeOnTheListStillStartsAnApplicationHostForEveryTest` | green; RED shown below |
| G3 | Passes on today's tree (G1 and G2 together, 126 entries) | the two above | green |
| G4 | Pure compare, newcomer only (made-up lists) | `ANewcomerOutsideTheListIsReportedAndNothingElse` | green |
| G5 | Pure compare, stale only | `AStaleEntryOnTheListIsReportedAndNothingElse` | green |
| G6 | Pure compare, lists agree in any order | `ListsThatAgreeReportNothingWhateverTheOrder` | green |
| G7 | Pure compare, both at once | `ANewcomerAndAStaleEntryAreReportedTogether` | green |
| G8 | The scan sees a host started per test however it is reached: per-test set-up, test body, helper called from set-up, lambda, after an `await`, derived host (`WithWebHostBuilder`); and does not flag a host started only in one-time set-up, or no host | `TheScanSeesAHostStartedForEveryTestHoweverItIsReached` (10 cases after Gate revision 1: adds a factory three steps below the named one, and an unrelated method with a sink's name) | green (10 cases) |
| G9 | The list only shrinks: it holds at most N entries, N = today's count (126); each slice lowers N in the same commit | `TheListOnlyShrinks` | green; RED shown in Gate revision 1 |

### Slice 1 (guard, measure, first move, cluster-substrate test)

| # | Scenario | Class |
|---|---|---|
| S1.1 | Same test ids before and after for the `ConnectionDefaultsAcceptanceTest` family (8 classes) and `ConnectionFieldChangeAcceptanceTest` | preservation |
| S1.2 | Each of those fixtures run alone 3 times in a row is green | preservation / state-leak check |
| S1.3 | No `[Ignore]`, `[Explicit]`, `Assert.Ignore`, `NonParallelizable` added; no `Assert.` line removed (diff grep) | E-weaken |
| S1.4 | The moved types are deleted from the list in the same commit; G1 and G2 stay green | ratchet |
| S1.5 | `ClusterSubstrateProbeRegistrationTest` still asserts: the check is registered, carries the startup tag, does not carry the ready tag; it no longer starts the app (its entry leaves the list) | preservation + D8 |
| S1.6 | Host-build time is reported per fixture for the untimed fixtures (D1) | measurement, read from the timing CSV, not a test |
| S1.7 | `BackendTestParallelizationGuardTest` green and unchanged | guard |

### Slices 2 to 5

| Slice | Scenarios |
|---|---|
| 2 template database | Same ids; the one-fixture A/B (a group B and a group C fixture, set-up mean falls at least 30 %); a test that expects a database without tables at first use (migration and health fixtures) still passes; the copied file is unique per use; an `IOException` on copy is retried then fails naming the path. The copy and retry logic has no mutants (test code), so its proof is the A/B and the full runs. |
| 3 four biggest | S1.1 to S1.4 for `TaskManager`, `ReconstructOverTimeHistory`, `FasterUpdates`, `UsageDataCollectorObservation`; the held pass runs on its own thread and the 60 s guard stays; checked with `DOTNET_PROCESSOR_COUNT=4` beside another class |
| 4 mid-size | S1.1 to S1.4 for the ten bases; `TeamFeatureSize`'s SQL-command counter is fixture-owned and reset in `[SetUp]` |
| 5 long tail | S1.1 to S1.4 per file by the 25 % rule; an entry that stays gets a written reason (body-bound or other); at the end the list is empty or every entry has a reason |
| End of track | 3 consecutive green full standard-filter runs (the two known publish-output scans excepted) |

Counts: 9 guard scenarios (18 test results, after the virtual-call planted case), plus the preservation shape repeated per slice. Failure-direction share: G1, G2, G4, G5, G7 and the
negative planted cases; the rest are preservation. Walking skeleton: N/A, because nothing a user does changes; the guard reading the real compiled
assembly is the end-to-end proof. Driven adapters: none new with real I/O.

## Wave: DISTILL / [REF] Acceptance criteria (Given / When / Then)

Driving port: the compiled test assembly read by `IlDependencies.ReadModule` (guard); the existing `dotnet test` run (preservation).

1. **Newcomer.** Given the list as committed; when a type starts a host in a `[SetUp]`, `[Test]`, `[TestCase]`, `[TestCaseSource]` or `[Theory]` method (or in anything those call in the test assembly) and it is not on the list; then the guard fails and names the type and the fix (host in `[OneTimeSetUp]`, per-test inputs reset in `[SetUp]`).
2. **Stale entry.** Given a type on the list; when it no longer starts a host per test; then the guard fails and says to remove it.
3. **Today's tree.** Given `main` as committed; when the guard runs; then both tests pass.
4. **Compare, driven with made-up lists.** Given a listed set and a found set; when they are compared; then newcomers are exactly found minus listed, stale are exactly listed minus found, order does not matter, and equal sets report nothing.
5. **Scan reaches the host however it is hidden.** Given a tiny type whose set-up starts a host directly / in a helper / in a lambda / after an await / by deriving one; when the scan reads the assembly; then the type is reported; a type that starts a host only in `[OneTimeSetUp]` is not.
6. **Behaviour preserved per converted fixture.** Given a fixture converted to a host per fixture; when its tests run; then the sorted test ids equal the ids before, no ignore, skip, explicit or serial marker is added, no assertion line is removed, and three runs of the fixture alone in a row are green.
7. **Parallel safety.** Given all converted fixtures; when the full standard-filter run is repeated; then three consecutive runs are green apart from the two known publish-output scans, and `BackendTestParallelizationGuardTest` is unchanged and green.

## Wave: DISTILL / [REF] Brief-AC traceability

| Brief AC | Scenario(s) / owner |
|---|---|
| Set-up share per harness measured before any change | **Lead-owned.** 109 timed fixtures done at DESIGN. For the 71 untimed files: D1 lands as the first, measurement-only commit of slice 1 (S1.6); the lead runs it once on a quiet machine and records the shares before any conversion (Gate revision 1, C2) |
| Hosts built per fixture, per-test inputs reset in `[SetUp]` | S1.1 to S1.4 per slice; ratchet G1, G2, G3 |
| Every backend test stays green, none deleted, skipped or weakened | S1.1, S1.3 per slice (E-names, E-weaken); AC 6 |
| Parallel safety: 3 consecutive green full runs, no new `[NonParallelizable]` | AC 7, **lead-owned run** at the end; S1.7 per slice |
| A guard stops it coming back (fails on a newcomer, fails on a stale entry, passes today) | G1 to G9 (written and live now) |
| Payoff: CI after-medians (summed seconds of moved classes plus their one-time set-up, step median of 7 runs) | **Lead-owned** (DEVOPS measurement protocol, after each push, rule set in advance in Gate revision 1, C3). No test asserts a time. |
| Mutation score of touched test helpers held | N/A, because Stryker.NET mutates the product assembly only (DEVOPS); the guard's equivalent is G4 to G9 |

## Wave: DISTILL / [REF] Test placement and scaffolds

- Placement: `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostPerFixtureGuardTest.cs`. Precedent: `PostgresContainerSharingGuardTest`, `BackendTestParallelizationGuardTest`, `RepositoryLeakRatchetArchUnitTest` (both-direction tests with a list, a pure compare), `IlDependencies` for the module reader (reused; the call walk is new and lives inside the guard file as a nested class).
- **Scaffold = the finished guard.** Nothing needs a stub: the guard compiles against code that exists, is live and green on today's tree, so no `[Ignore]` is used.
- Entries are keyed by the **full name of the top-level type** that holds the `[SetUp]` or test method (Gate revision 1, A3). Four reasons are in use: `NotMovedYet` (117), `OnAPostgresServer` (5), `ChecksTheTestHostItself` (3), `HasOneTestOnly` (1).
- How the scan works (so DELIVER can trust or fix it): roots are methods with the five NUnit attributes; calls, constructors, `ldftn` delegates and the `MoveNext` of async and iterator methods are followed inside the test assembly; a virtual call on a type written in the assembly also reaches its overrides; the factory types themselves are not walked into (they are the sink); a method whose whole closure is clean is remembered.
- Known limits (not fixed): interface calls are not resolved to their implementations; methods overloaded with the same name and argument count are treated as all of their overloads (can only over-report); a host started through reflection, or through a factory type that is not a `TestWebApplicationFactory`/`WebApplicationFactory` subclass, is not seen. A call to `WithWebHostBuilder` or `WithTestAuthentication` counts only when it is made on a factory type (Gate revision 1, A4). The Release assembly was compared with Debug (Gate revision 1, A6): identical list, now a per-slice pre-push gate.
- Edits DELIVER makes per slice: delete the moved types' lines from `TypesAllowedToBuildAHostPerTest`; nothing else in the guard changes. When the dictionary is empty, replace it with a plain "no type" assertion and drop the stale test.

## Wave: DISTILL / [REF] Allow-list at the start (the ratchet's starting point)

**126 types.** Of those, 3 are `OnAPostgresServer`, 3 are `ChecksTheTestHostItself`, and 120 are `NotMovedYet`.

PICK-UP counted **80 files** with a classifier (indentation and attribute heuristic; direct construction in `[SetUp]` or a test body; helpers not followed).
Comparison of the two sets, file by file (each of the 126 types lives in its own file, so types and files are both 126):

| Set | Files |
|---|---|
| Classifier's 80 that the scan also reports | 79 (the 80th, a TaskManager specifications file, belongs to a type the scan lists under its other file) |
| Reported by the scan, classified earlier as helper, constructor, class-level or unknown | 13 |
| Reported by the scan, not seen at all by the classifier | 34 (of them 5 under `Integration/Containers/`, which the classifier excluded) |
| **Total** | **126** |

So the difference of **46** is what the PICK-UP re-count said it could not settle: hosts reached through a helper, a base class, a `*TestHost` object or a
constructor called from a per-test method. The scan follows those; the classifier did not. All 34 unseen files were then read one by one (Gate revision 1, A7): 34 confirmed, 0 wrongly listed, N stays 126.
The 13 the classifier saw as helper, constructor or unresolved were not read one by one; one that proves not to start a host per test comes off the list in the slice that finds it.
Against the brief's 95 (files building a `TestWebApplicationFactory` host per test): 126 is higher because the brief counted construction sites and this counts
types that can reach one. The ratchet starts at 126 and may only shrink.

## Wave: DISTILL / [REF] RED classification (run 2026-11-01)

Both runs: `dotnet build -v q` (0 warnings), `LIGHTHOUSE_TEST_WORKERS=6 dotnet test --no-build --filter "FullyQualifiedName~TestHostPerFixtureGuardTest"`; the file was restored after each and rebuilt.

| Case | Change made temporarily | Result | Classification |
|---|---|---|---|
| Newcomer | deleted the `TaskManagerAcceptanceTest` line from the list | G1 failed, 13 passed | MISSING_FUNCTIONALITY of the list (correct RED): the assertion fires on the real offender; no setup or compile error |
| Stale | added `["ABogusTypeThatWasFixedLongAgo"] = NotMovedYet` | G2 failed, 13 passed | correct RED |
| First version | planted fixtures were `internal sealed` | NUnit ran their `[Test]` methods, one failed with a null reference | fixed: `abstract` |

Newcomer, as printed:

```
Failed NoTypeStartsAnApplicationHostForEveryTestWithoutBeingOnTheList [34 ms]
  Error Message:
     These types start an application host in a per-test set-up or in a test body: TaskManagerAcceptanceTest. Build the host in [OneTimeSetUp] and dispose it in [OneTimeTearDown]; reset what differs per test in [SetUp] through objects the fixture owns. If a type genuinely needs a host of its own per test, add it to the list with the reason.
Assert.That(comparison.Newcomers, Is.Empty)
  Expected: <empty>
Failed!  - Failed:     1, Passed:    13, Skipped:     0, Total:    14, Duration: 163 ms
```

Stale, as printed:

```
Failed EveryTypeOnTheListStillStartsAnApplicationHostForEveryTest [24 ms]
  Error Message:
     These types are on the list but no longer start a host per test: ABogusTypeThatWasFixedLongAgo. Remove them from the list so it cannot grow back unseen.
Assert.That(comparison.Stale, Is.Empty)
Failed!  - Failed:     1, Passed:    13, Skipped:     0, Total:    14, Duration: 99 ms
```

Final state: `Passed! - Failed: 0, Passed: 16, Skipped: 0, Total: 16` (14 in the new fixture, 2 in `BackendTestParallelizationGuardTest`).

The 8-case table, the 16-test count and the simple-name messages above are the first run. Gate revision 1 re-ran them with full-name keys and the ceiling (17 tests in the fixture, 19 with the parallelization guard), and broke every branch of the scan:

#### Branch-breakage table (Gate revision 1, A1; extended in DELIVER prep with `OverridesOf` returning nothing and the no-host memo never caching)

For each branch of the scan I broke one line, rebuilt (Debug, 0 warnings except the planned failure), ran
`LIGHTHOUSE_TEST_WORKERS=4 dotnet test --no-build --filter "FullyQualifiedName~TestHostPerFixtureGuardTest"` and restored the line.
Names are as printed. A line is a failure of the planted-case test unless noted; the guard fixture had 17 tests for the first eight rows and has 18 since the virtual-call case (the last two rows were run with 18).

| Branch | Line broken | Failed (as printed) | Passed / total |
|---|---|---|---|
| ldftn / ldvirtftn (lambda) | dropped `Code.Ldftn` and `Code.Ldvirtftn` from the opcodes that are followed | `TheScanSeesAHostStartedForEveryTestHoweverItIsReached("StartsAHostInALambda",True)` | 16 / 17 |
| compiler-made bodies (after-await) | looked for `<Never...>` instead of `<Method>` nested types | `EveryTypeOnTheListStillStartsAnApplicationHostForEveryTest`; `TheScanSeesAHostStartedForEveryTestHoweverItIsReached("StartsAHostAfterAnAwait",True)` | 15 / 17 |
| helper following | dropped `Code.Call` and `Code.Callvirt` from the opcodes that are followed | `EveryTypeOnTheListStillStartsAnApplicationHostForEveryTest`; `...("StartsAHostInAHelperTheSetUpCalls",True)`; `...("DerivesAHostInATestBody",True)` | 14 / 17 |
| factory-subclass growth | the loop stopped repeating (`grew \|= ...` became a plain add) | `TheScanSeesAHostStartedForEveryTestHoweverItIsReached("StartsAHostFromAFactoryThreeStepsBelowTheNamedOne",True)` | 16 / 17 |
| derived-host sink (`WithWebHostBuilder`) | the sink names became `"NeverASink"` | `EveryTypeOnTheListStillStartsAnApplicationHostForEveryTest`; `...("DerivesAHostInATestBody",True)` | 15 / 17 |
| A4 narrowing (sink only on a factory type) | removed the `IsAFactoryType(called.DeclaringType)` condition | `NoTypeStartsAnApplicationHostForEveryTestWithoutBeingOnTheList`; `...("CallsAnUnrelatedMethodWithTheSameNameAsAHostSink",False)` | 15 / 17 |
| Compare (swap newcomers and stale) | `foundNames.Except(listedNames)` became `listedNames.Except(foundNames)` | G4 `ANewcomerOutsideTheListIsReportedAndNothingElse`, G5 `AStaleEntryOnTheListIsReportedAndNothingElse`, G7 `ANewcomerAndAStaleEntryAreReportedTogether` (G6 and the real-tree tests stay green: equal sets give empty either way) | 14 / 17 |
| Ratchet ceiling | `MostTypesTheListMayHold = 125` | `TheListOnlyShrinks` | 16 / 17 |
| Overrides of a virtual call (`OverridesOf`) | the method returned `[]` | `TheScanSeesAHostStartedForEveryTestHoweverItIsReached("StartsAHostOnlyThroughAVirtualCall",True)`; the real-tree tests stay green because no listed type depends on a virtual call alone | 17 / 18 |
| No-host memo (`knownToStartNoHost`) | the memo was never filled, so every root is walked afresh | none: all 18 passed. The memo only saves time, it cannot change an answer, so no test can see it; deliberately not pinned | 18 / 18 |

Two lessons the table taught, both fixed before it was recorded: (1) the first "factory-subclass growth" case had a grandchild
whose parent was already a factory by name, so a single pass found it and breaking the loop changed nothing; the planted chain is now
three steps deep and declared bottom-up, so only a repeating loop finds the third step. (2) One of my break expressions did not
compile and the run used the previous binary; every row above was rebuilt and checked for `0 Error(s)` before it counts.

#### Newcomer and stale RED, verbatim (re-run with full-name keys and the ceiling)

Newcomer (line for `TaskManagerAcceptanceTest` removed from the list):

```
  Failed NoTypeStartsAnApplicationHostForEveryTestWithoutBeingOnTheList [24 ms]
  Error Message:
     These types start an application host in a per-test set-up or in a test body: Lighthouse.Backend.Tests.API.Integration.TaskManager.TaskManagerAcceptanceTest. Build the host in [OneTimeSetUp] and dispose it in [OneTimeTearDown]; reset what differs per test in [SetUp] through objects the fixture owns. If a type genuinely needs a host of its own per test, add it to the list with the reason.
Assert.That(comparison.Newcomers, Is.Empty)
  Expected: <empty>
  But was:  < "Lighthouse.Backend.Tests.API.Integration.TaskManager.TaskManagerAcceptanceTest" >

  Stack Trace:
     at Lighthouse.Backend.Tests.Architecture.TestHostPerFixtureGuardTest.NoTypeStartsAnApplicationHostForEveryTestWithoutBeingOnTheList() in /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostPerFixtureGuardTest.cs:line 180

1)    at Lighthouse.Backend.Tests.Architecture.TestHostPerFixtureGuardTest.NoTypeStartsAnApplicationHostForEveryTestWithoutBeingOnTheList() in /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostPerFixtureGuardTest.cs:line 180



Failed!  - Failed:     1, Passed:    17, Skipped:     0, Total:    18, Duration: 138 ms - Lighthouse.Backend.Tests.dll (net10.0)
```

Stale (an entry `Lighthouse.Backend.Tests.ABogusTypeThatWasFixedLongAgo` added; it also takes the list to 127, so the ceiling fails too, as it should):

```
  Failed EveryTypeOnTheListStillStartsAnApplicationHostForEveryTest [24 ms]
  Error Message:
     These types are on the list but no longer start a host per test: Lighthouse.Backend.Tests.ABogusTypeThatWasFixedLongAgo. Remove them from the list so it cannot grow back unseen.
Assert.That(comparison.Stale, Is.Empty)
  Expected: <empty>
  But was:  < "Lighthouse.Backend.Tests.ABogusTypeThatWasFixedLongAgo" >

  Stack Trace:
     at Lighthouse.Backend.Tests.Architecture.TestHostPerFixtureGuardTest.EveryTypeOnTheListStillStartsAnApplicationHostForEveryTest() in /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostPerFixtureGuardTest.cs:line 195

1)    at Lighthouse.Backend.Tests.Architecture.TestHostPerFixtureGuardTest.EveryTypeOnTheListStillStartsAnApplicationHostForEveryTest() in /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostPerFixtureGuardTest.cs:line 195


  Failed TheListOnlyShrinks [4 ms]
  Error Message:
     The list of types allowed to start a host for every test can only get shorter. Fix the type instead of adding it, and when a type leaves the list, lower the number above by the same amount.
Assert.That(TypesAllowedToBuildAHostPerTest, Has.Count.LessThanOrEqualTo(MostTypesTheListMayHold))
  Expected: property Count less than or equal to 126
  But was:  127

  Stack Trace:
     at Lighthouse.Backend.Tests.Architecture.TestHostPerFixtureGuardTest.TheListOnlyShrinks() in /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostPerFixtureGuardTest.cs:line 205

1)    at Lighthouse.Backend.Tests.Architecture.TestHostPerFixtureGuardTest.TheListOnlyShrinks() in /storage/repos/Lighthouse/Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostPerFixtureGuardTest.cs:line 205



Failed!  - Failed:     2, Passed:    15, Skipped:     0, Total:    17, Duration: 88 ms - Lighthouse.Backend.Tests.dll (net10.0)
```

## Wave: DISTILL / [REF] Definition of Ready (DISTILL)

| Item | Status | Evidence |
|---|---|---|
| Reconciliation, 0 contradictions | PASS | above |
| 4 UAT G/W/T | PASS | the seven above |
| Every brief AC maps to a scenario or an owner | PASS | traceability; payoff, the 3 full runs and the set-up share are lead-owned |
| Failure-direction share | PASS | the guard is all failure-direction scenarios (G1, G2, G4, G5, G7, the negative planted cases) |
| Test placement with precedent | PASS | `Architecture/` guard fixtures |
| Scaffolds green at hand-off | PASS | build 0 warnings; 16 passed, 0 skipped; no `[Ignore]` (the count after Gate revision 1 and round 2: 18 passed in the guard fixture, 20 with `BackendTestParallelizationGuardTest`, run on 2026-11-02) |
| RED classified as the right reason | PASS | both directions printed above |
| No scenario tags, no internal refs in code | PASS | checked in the new file |
| Open for DELIVER | NOTE | Release-assembly comparison done (Gate revision 1, A6: identical); the Windows copy risk and D8's Program.cs push cost stay as written |

## Wave: DISTILL / [REF] Gate revision 1 (2026-11-01)

Applies the lead's decisions after the four-reviewer gate. The reviewers' own finding ids were not passed to the designer, so the
"Findings resolved" column states the gap each decision closes in plain words. Code lives in one commit, docs in another;
the guard's own file and the measurement scripts are the only code touched. Nothing was pushed.

| Decision | Findings resolved | Where changed |
|---|---|---|
| A1 proof per branch | The scan's branches were each shown only by one planted case; nobody had broken a branch and watched a case fail | "Branch-breakage table" below; two planted cases added in the guard (a factory three steps below the named one; an unrelated method with a sink's name) |
| A2 ratchet | "The list only shrinks" was a promise, not a test | Guard: `MostTypesTheListMayHold = 126` and `TheListOnlyShrinks`; message says the list only shrinks |
| A3 full names | Two types with the same simple name would share an entry | Guard list keyed by full type name (126 entries rewritten); this file's "Test placement" and "Allow-list" sections |
| A4 sink limit | `WithWebHostBuilder` and `WithTestAuthentication` matched by name on any type | Narrowed in the guard: a sink counts only when called on a factory type (the family `IsAFactoryType` already knew). Cheap, so narrowed rather than listed as a limit; list unchanged at 126 |
| A5 comments | Planted-types comment gave the wrong reason; a loop comment narrated | Guard: comment says abstract so NUnit does not run them; the loop is rewritten as a `do ... while (grew)` with no comment; no ids or tags in comments |
| A6 Release parity | DEVOPS asked for it; not done | "Release against Debug" below; becomes a per-slice pre-push gate |
| A7 reconcile and examine | 63 + 18 = 81 versus 80; 95 versus 126; 34 types never examined; two Containers types still "not moved yet" | "Reconciliation of counts" and "The 34 types, one by one" below; Containers reasons set |
| A8 scaffold | Commit 827ab7eb8 is a scaffold that could be mistaken for a pushable unit | See "Commit rule" below |
| B1 clock | `FakeTimeProvider` is created inline in the factory; a converted fixture would share one clock across tests | D3 rewritten (below); scenario DB-1 |
| B2 database name | D3 named a dependency that is not the real one | D3 and D4 rewritten; scenario DB-2; recorded check |
| B3 rate limiting | Rate-limiter partitions are state a shared host keeps | D3 reset list; scenario DB-3 |
| B4 template safety | Template build and copy could race across test processes | D4 rewritten; scenario DB-4 (entry condition for slice 2) |
| B5 teardown order | Order of host disposal and file deletion unspecified; disk high-water mark unknown | D3; scenario DB-5; Windows check moved |
| B6 A/B gate | The 30 % threshold was not pre-registered per group | D4; "A/B gate" below |
| C1 savings are ceilings | Savings read like promises; slice 1 dropped the database share; "biggest first" was not the rule applied | "Savings, as ceilings" below; D2 order rule restated |
| C2 set-up before change | Untimed set-up shares were to be measured "before any change" with no commit that does it | Slice 1 order; AC traceability reworded |
| C3 CI KPI | The metric the "after" is compared with was never checked against how the artifact is made | "CI KPI and decision rule" below, with the measured trx finding |
| C4 memory | Memory was a "watch", with no number | "Memory stop threshold" below |
| C5 forced-full push | The slice-1 push runs a bigger suite than announced | "Forced-full push" below |
| C6 CI parity | Local runs at 6 workers; CI differs; green was loosely defined | "Parity and order" below |
| C7 scripts | Scripts had hard-coded paths and a median that counted absences as zero | `cistats.py` and `classify.py` rewritten to take arguments |
| C8 behaviour-preservation commands | Described in prose only | "Behaviour-preservation commands" below; script `measurement/scripts/behaviour-preserved.sh`, before-list `measurement/test-ids-before.2026-11-01.txt.gz` |
| D1 sizing | Slices were "rough sizes" | "Slice sizes" below |
| D2 counts | DoR item 1 quoted a count the guard does not use | DoR re-run row 1 patched; reconciliation below |
| D3 slice 5 | Optional status and fall-back not stated | "Slice sizes" below |
| D4 re-sizing | Slices 3 to 5 sized on the proxy | "Slice sizes" below |
| D5 brief edits | Brief lacked the infrastructure rationale and had a mutation AC the track cannot meet | Brief patched (`slice-42-t2-backend-host-per-fixture.md`) |
| D6 owner | "The lead" was unspecific | DEVOPS protocol header and the line below |
| D7 titles | Guard test names are method-style | Noted below |
| D8 fallback | Needed restating | Stays taken; see below |

### Commit rule (A8)

The scaffold commit `827ab7eb8` is the first local commit of slice 1 and is **never pushed on its own**. Slice 1 is pushed as one unit:
that scaffold, this revision's code commit, the measurement-only D1 commit, and the conversions. Nothing in the scaffold is
valid alone, because its list was counted by the first run and has since been re-keyed and ratcheted.

### Branch-breakage table and verbatim REDs (A1)

Moved into the RED classification section above (subsections "Branch-breakage table" and "Newcomer and stale RED, verbatim"), where the earlier REDs are.

### Release against Debug (A6)

`dotnet build -c Release -v q`: 0 warnings, 0 errors. `LIGHTHOUSE_TEST_WORKERS=4 dotnet test -c Release --no-build --filter "FullyQualifiedName~TestHostPerFixtureGuardTest|FullyQualifiedName~BackendTestParallelizationGuardTest"`:
`Passed! - Failed: 0, Passed: 19, Skipped: 0, Total: 19`. The Debug run of the same filter gives the same 19 of 19. Both guard directions pass in both
configurations against the same 126 entries, so the offender list is identical: no type is found only in Debug and none only in Release.
**Per-slice pre-push gate:** the guard filter green under Debug and under `-c Release` (CI tests Release), both after a fresh build.

### Reconciliation of counts (A7)

| Number | What it counts | Reconciled |
|---|---|---|
| 80 (PICK-UP) | files where the classifier saw the host built in `[SetUp]` or a test body | `SetUp` 63 files + `test-body` 18 files = 81, minus 1 file that is in both (`RbacGroupMappingConcurrencyTokenIntegrationTest`) = **80** |
| 95 (brief) | files, by a different pattern, counted in October | a file count; not comparable with types |
| 111 / 112 | files containing `new TestWebApplicationFactory` | 111 at `b2b516258`; **112** now, because the guard's own planted fixtures add the guard file |
| 132 (133 now) | files matching `new TestWebApplicationFactory`, `WithWebHostBuilder(` or `WithTestAuthentication(` | the classifier's universe: its output has 140 lines, 7 summary lines, one blank and 132 files at `b2b516258` (133 now, the guard file) |
| **126** | **types** whose per-test methods reach a host start (the guard) | 80 classifier files; of the guard's 126, 79 of those 80 files are listed (the 80th is a TaskManager specifications file whose type is listed under its other file); + 13 the classifier saw as helper, constructor, class-level or unresolved; + 34 it did not see; = **126** |

DoR item 1 now cites **126 types** (80 by the classifier; the other 46 are hosts reached through a helper, a base class, a `*TestHost` or a constructor).

### The 34 types the classifier did not see, one by one (A7)

For each I read the set-up, tear-down and test bodies (the file, its base class and the `*TestHost` it builds). Verdict for all 34: **confirmed**
a host is started for every test (or lazily in the test body for every test); **0 wrongly listed**. N stays **126** after the examination.

| Group | Types | What starts the host, per test | Verdict |
|---|---|---|---|
| `*TestHost` built in each test body (7) | `ForwardedHeadersIntegrationTest` (10 tests), `HealthCheckIntegrationTest` (7), `ShutdownIntegrationTest` (4), `JwtBearerAuthIntegrationTest` (5), `McpInboundAuthIntegrationTest` (8), `MetricsEndpointIntegrationTest` (4), `ServiceProviderValidationTest` (1) | `using var host = new <X>TestHost(options)` in each test: `ForwardedHeadersTestHost`, `JwtBearerTestHost`, `McpInboundAuthTestHost` build a `TestWebApplicationFactory` plus `WithWebHostBuilder`; `HealthCheckTestHost` and `TelemetryTestHost` build a plain `WebApplicationFactory<Program>().WithWebHostBuilder`; `ServiceProviderValidationTest` builds its own `ValidatingFactory` in its one test | confirmed. `ServiceProviderValidationTest` has one test, so a host per fixture saves no host: it stays with the reason `HasOneTestOnly` |
| Blackout forecast bases (8) | `BlackoutForecastShiftItemPredictionIntegrationTest`, `BlackoutForecastShiftTeamForecastIntegrationTest`, `RecurringBlackoutRulesByDateForecastIntegrationTest`, `...DeliveryIntegrationTest`, `...DownstreamParityIntegrationTest`, `...IntervalRuleIntegrationTest`, `...LifecycleIntegrationTest`, `...WeekendsForeverIntegrationTest` | each `[SetUp]` calls `StartApplicationWithDeterministicForecast()` of its base, which builds `new TestWebApplicationFactory<Program>()`, `WithTestAuthentication` and `WithWebHostBuilder`; `[TearDown]` stops it | confirmed |
| Viewer embed (5) | `S9_EmbedCookiePolicyTests`, `S9b_EmbedCookieConcurrencyTests`, `S13_ViewerEmbedHandshakeTests`, `S14_ViewerEmbedSubjectValidatorTests`, `S15_EmbedSurfaceOptInTests` | `[SetUp]`: `host = new ViewerEmbedTestHost()`, which builds a root factory and several derived hosts (auth enabled, auth disabled, licence blocked, misconfigured, embed disabled) | confirmed; the heaviest of the group (one root and up to five derived hosts per test) |
| Named dependency link (4) | `Slice01JiraDependenciesFromANamedLinkScenarios`, `Slice02NamedLinkThatMatchedNothingScenarios`, `Slice03AzureDevOpsRelationTypeAcceptedScenarios`, `Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios` | the base `[SetUp]` only forgets the last scenario's fakes; `TheRunningLighthouse()` builds `new TestWebApplicationFactory<Program>()` plus `WithWebHostBuilder` on first use inside each test, and the base `[TearDown]` stops it | confirmed (lazy, in the test body) |
| Parent placement (3) | `Slice01NarrowToFeaturesTest`, `Slice02OldestLinkWinsTest`, `Slice03PortfolioOldestLinkTest` | same lazy pattern in `ParentPlacementAcceptanceTest.TheRunningLighthouse()` | confirmed |
| Parent from issue links (1) | `Slice03AmbiguityRefusalTest` (partial) | the specifications half has `[SetUp]` and builds `new TestWebApplicationFactory<Program>()` in a helper each test uses | confirmed |
| Task manager (5, partials) | `Slice02SeeWhatIsRunningTest`, `Slice04StopARefreshTest`, `Slice05AnyBrokenCredentialSaysSoTest`, `Slice06TheWarningsWithoutTheLogTest`, `Slice08HealthThatChecksItselfTest` | the base `TaskManagerAcceptanceTest.Init()` (`[SetUp]`) does `RootFactory = new TestWebApplicationFactory<Program>()`; test helpers derive hosts with `RootFactory.WithWebHostBuilder` | confirmed |
| Containers (1) | `EmbedSessionSingleUseConcurrencyTests` | each of its two tests builds `new PostgresEmbedFactory(postgres.GetConnectionString())` over a Postgres container the test starts | confirmed; reason `OnAPostgresServer` |

Containers folder: all five listed types now carry the Postgres-server reason (the three `...OnPostgres` fixtures, `EmbedSessionSingleUseConcurrencyTests`,
and `ScalabilityTests`, most of whose tests start a Postgres container or a Redis store of their own around a host). The reason text names "a Postgres server
(or a Redis store)". Zero entries in `Integration/Containers/` say "not moved yet". Reasons in use now: `NotMovedYet` 117, `OnAPostgresServer` 5,
`ChecksTheTestHostItself` 3, `HasOneTestOnly` 1.

Not examined, and said so: the 13 types the classifier saw as helper, constructor, class-level or unresolved were not read one by one; the 92 types the
classifier saw (79 + 13) are accepted on the scan. They come off the list the first time a slice shows one does not start a host per test.

### D3 and D4, made concrete (B1 to B6)

**D3, the per-fixture pattern, with the state a shared host keeps.** `[OneTimeSetUp]` builds the host and `[OneTimeTearDown]` disposes it, as before. What is
reset per test, in `[SetUp]`, through objects the fixture owns:

- **Clock (B1).** `TestWebApplicationFactory` owns one `FakeTimeProvider` field, registers that instance in `ConfigureWebHost` (it creates one inline today) and
  exposes it. A converted fixture resets it in `[SetUp]` to `TestToday.FrozenNow`. The fixtures that move the host's clock are found with
  `git grep -nE "\.Advance\(" -- '*.cs'` (20 files) and then `git grep -nE "GetRequiredService<TimeProvider>|\(FakeTimeProvider\)"`: only **two move the host's own clock**, and each
  goes in the slice that converts it:
  - `NamedDependencyLinkAcceptanceTest` (line 790, through `TheRunningLighthouse()`): the family of 4 listed types above; converted in the slice that converts them
    (re-sized after D1, see "Slice sizes"); until then they stay listed (`NotMovedYet`).
  - `Slice02ConfirmedNowScenarios` (line 154, through `Factory`): reached through `SkipUnchangedForecastsAcceptanceTest` and `TaskManagerAcceptanceTest`, so it is part of
    slice 3's TaskManager family.
  The other 18 files advance a clock they built themselves (`S8_EmbedEntryPointTests`, `LighthouseClockTest` pass their own to a host; the rest are unit tests): no shared-host risk.
- **Database name (B2).** The database file name is read **per scope**, not once: `AddDbContext`'s options callback is scoped, so it reads `databaseFileName` each time a scope builds
  its options. `UseFreshDatabase()` therefore changes the file only for scopes created **after** it. A converted fixture must not resolve `LighthouseAppContext` or
  `DbContextOptions<LighthouseAppContext>` from the root provider, because a root-resolved context is built once and keeps the old file. Recorded check, run in every slice that converts
  a fixture, today **0 hits across the whole test project**:
  `git grep -nE '\.Services\.(GetRequiredService|GetService)<(LighthouseAppContext|DbContextOptions<LighthouseAppContext>)>' -- '*.cs'` must print nothing. (A guard case was weighed
  and left out: the pattern is a plain text search with no false positives today, and a guard over service resolution would need its own planted types.)
- **Rate limiting (B3).** The rate-limiter partitions (counters per client address) live in the host and survive across tests of a shared host. Hosts default to `RateLimits:Enabled=false`
  (T1). A fixture that tests rate limiting keeps one host per configuration and builds a derived host **per test** where its counters matter, or stays listed with a reason. The fixtures
  that switch it on and are listed: `S6_RateLimitingTests`, `UsageDataConsentRateLimitTests`, `UsageDataEventCeilingTests`, `S9b_EmbedCookieConcurrencyTests` (through `ViewerEmbedTestHost`).
  Each decides in its own slice; the default is "stays listed, reason `counters are per host`".
- **Teardown order (B5).** `[OneTimeTearDown]` disposes the host **before** deleting database files (the factory's `Dispose` already deletes them after `base.Dispose`; the
  fixture must not delete first). Database files are removed at **fixture end**, not per test, so a fixture's files pile up while it runs. Slice 1 measures the disk
  high-water mark once (peak total size of `IntegrationTests_*.db*` in the working folder during a full run) and records it.

**D4, the template database, made safe across processes (B4).** The template is a pure optimisation of set-up; its rules:

- built under a **temporary unique name** (`template_<guid>.tmp`), completed (`Database.EnsureCreated()`, WAL checkpointed away, no `-wal`/`-shm`, connection closed), then `File.Move`d
  atomically to a **per-content or per-run name** (a hash of the model's snapshot, or the process id plus start time);
- a copy is made **only from a finished file** (the final name exists only after the move);
- every copy has a **unique destination** (`NewDatabaseFileName`), so two tests never share one;
- the copy happens inside the options callback, the first time a scope reads a file name that does not exist yet; `UseFreshDatabase()` keeps working because it only swaps the name.
- **Entry condition for slice 2:** scenario DB-4 below is written and green before the template becomes the default.

**A/B gate, pre-registered (B6).** Before the template is the default, per group: the **median of at least 5 runs per fixture at the same worker count**, one group-B fixture and
one group-C fixture. Threshold: **B at least 30 %, C at least 30 %** fall in set-up mean. If the groups disagree, the template ships **only for the group that passed**
(it switches per base class). The seeded variant (D4b) is decided only if the schema-only template passes in **both** groups. The numbers are recorded in DELIVER.

### Scenarios added (B1 to B5)

Preservation scenarios for the slices; none is a new test in this commit (the converted fixtures do not exist yet). They become tests or run commands in the slice that needs them.

| # | Scenario | Slice | Proof |
|---|---|---|---|
| DB-1 | An `Advance` of the host clock in one test is not visible in the next test of the same fixture | the first slice that converts `NamedDependencyLinkAcceptanceTest` or the TaskManager family | a test in the converted fixture that advances the clock, followed by one that asserts `TestToday.FrozenNow` (alphabetical order is fixed, so name them to run in that order) |
| DB-2 | A scope created after `UseFreshDatabase()` sees the new database file, and a scope created before it still sees the old one | slice 2 (the template) and every slice with `UseFreshDatabase` | a test in `TestWebApplicationFactoryTest` using two scopes; plus the recorded grep above |
| DB-3 | A fixture that tests rate limiting gets a fresh counter per test | the slice that converts one of the four fixtures | the converted fixture's own tests: the second test must not hit the first test's limit |
| DB-4 | Two processes build the template at the same time and both get a complete, identical copy | slice 2, entry condition | a test that starts the build from two tasks (and one subprocess) at once and compares the copies' schemas; the final name never exists half-written |
| DB-5 | `[OneTimeTearDown]` disposes the host before database files are deleted, and no file is left after the fixture | slice 1 onwards | after a converted fixture, no `IntegrationTests_*.db*` remains in the working folder (`ls` check in the slice's local checks) |

### Savings, as ceilings (C1)

Every figure below is an **unmeasured ceiling**. The step projection too. None is a promise; the measured before and after (C3) decide.

| Slice | Ceiling (CI-summed seconds) | How computed | Assumed shares |
|---|---|---|---|
| 1. Guard + measure + first move | about **65 s**, plus the Cluster test (0.8 to 16.4 s) | 58 s local set-up of the moved family x (1 - 153/409) x 1.8 CI factor = 36.3 x 1.8; the database share is kept (group B's 153 ms stays) | CI factor 1.8 (calibrated on 12 fixtures) |
| 2. Template | about 150 s (schema-only); up to about 380 s with seeding | 50 ms x about 2,500 database-per-test tests (x 1.2) | 50 ms from group C's 56 ms; the 2,500 count |
| 3. Four biggest untimed | about 180 s | 60 % of 305 s proxy | the 60 % remaining share and the proxy itself |
| 4. Mid-size bases | about 150 s | 60 % of about 250 s proxy | same two |
| 5. Long tail | about 50 s | 60 % of the about 120 s proxy, less what the 25 % rule leaves listed | same two |

The old "-400 s" figure for the untimed 71 files is retracted as a number to promise: it is a ceiling built on two assumed shares (the proxy and the 60 %) and
slices 3 to 5 above are its parts. **Corrected total ceiling: about 595 to 610 CI-summed seconds with the schema-only template (about 21 % of 2,806 s), up to about 830 with seeding.**
CI runs about 3.8 tests in parallel (2,806 s / 746 s), so the step ceiling is about **-160 s (746 to about 590), up to about -220 s with seeding**; per slice about
-17 to -22 s, -40 s (-100 s), -48 s, -40 s, -13 s. The step median ranges 559 to 893 s and is bimodal, so no single slice is likely to show a 60 s step drop; the moved-class
criterion of C3 is the one most slices can meet. The earlier "620-850 s, 2.5-3.5 min" is superseded.

**Order rule actually applied:** the guard goes first (slice 1 carries it, so the list stops growing at once); after that, **measured before proxy** (slice 1's family has timed set-up;
slices 3 to 5 have only the proxy until D1 measures them); the template's A/B (B6) gates its size and whether it ships per group. "Biggest first" is dropped: the proxy over-states
where a class has no cheap test, so it is a ranking aid, not an order.

### Set-up before change (C2)

D1 (host-build time into `FixtureSetupTimer`, kind `HostBuild`, attributed to the running fixture) lands as the **first, measurement-only commit of slice 1**, after the scaffold and before
any conversion. The lead then runs it **once, when the machine is quiet** (`LIGHTHOUSE_FIXTURE_TIMING=1`, standard filter, 6 workers) and records the untimed set-up shares in
`measurement/` and here before the first conversion. The brief's AC "set-up share recorded per harness before any change" is owned by that run for the 71 untimed files
(the 109 timed fixtures are already recorded); see the reworded traceability row.

### CI KPI and decision rule (C3)

**How the artifact is made.** `.github/workflows/ci_backend.yml` runs `dotnet test ... --logger trx`; step "Extract backend per-test timings" runs
`Scripts/test-timings/trx_to_csv.py`, which writes one row per `UnitTestResult` (`fully_qualified_name, category, duration_ms, outcome`) into `test-timings-backend.csv`
(artifact `test-timings-backend`). It has no start times and no fixture rows.

**Does the artifact include `[OneTimeSetUp]` time? No.** Measured on this machine with `--logger trx` on `Slice13ReadinessTest` (44 tests, a group-B fixture; with
`LIGHTHOUSE_FIXTURE_TIMING=1`): the timer reports one-time set-up 2,166 ms and per-test set-up 6,979 ms (mean 159 ms x 44); the trx's per-test durations add up to 10.75 s and the span from
the first test's start to the last test's end is 10.77 s, so the two are within 0.01 s of each other: there is **no gap inside the span for the one-time set-up**. The 2.17 s sits in the 3.74 s
between run start and the first test, belonging to no test. Per-test `[SetUp]` time **is** inside each test's duration; `[OneTimeSetUp]` and `[OneTimeTearDown]` time is in none.
Consequence: when a slice moves a host from `[SetUp]` to `[OneTimeSetUp]`, the per-class sums drop by the whole host cost and never show the one build that remains.
The overstatement is one host build (about 0.2 to 2 s) per moved fixture.

**KPIs.**
- **Primary:** the backend test step median (`Test Backend (with Coverage)`, as the baseline in `measurement/ci-backend-test-step-seconds.tsv`, median 746 s over 23 runs).
- **Secondary:** suite-level time per moved class = the class's summed per-test seconds from the artifact (the baseline's definition, so no recompute of "before") **plus** the fixture's one-time
  set-up and tear-down from the D1 timing run (local, the lead's, multiplied by the 1.8 CI factor), added to the "after" side only. Per-test sums alone would credit the slice with a saving
  the suite does not get.

**Decision rule, set in advance.** n = **7** `main` runs per slice (as the baseline); report every value and both modes of the step time (about 560 to 720 s and 830 to 890 s).
A slice counts as a **measured win** if the step median drops by **at least 60 s**, or the moved classes' suite-level median drops by **at least 50 % of the slice's ceiling** (slice 1: 33 s; slice 2: 75 s;
slice 3: 90 s; slice 4: 75 s; slice 5: 25 s). Otherwise it is reported as a **failed prediction**, with the numbers. The **slice-1 run is excluded from the after set**, because the
`Program.cs` change forces the full Integration suite on that push (see below), so its step time is not comparable.

### Memory stop threshold (C4)

Baseline: T1's after measurement on this machine, **4.3 GB peak at 6 workers, standard filter, commit `b573b1924`**. Stop threshold: a full run above **4.73 GB (+10 %)** stops the track.
It is **step 4a of the after protocol** (the lead's run on a quiet machine, same worker count and filter). CI memory is **not measured**; no number from CI is claimed. T1 named and fixed both retention roots
(the per-request `IHttpClientFactory` handler lifetime and the rate-limiter timers), so a host that lives for a whole fixture is expected to be released at fixture end; the
`TestHostIsReleasedAfterDisposeTest` guard proves that release for one host.

### Forced-full push (C5)

`ci_backend.yml` (line 134), for a change under a shared path such as `Program.cs`: the test filter becomes `(Category=Integration&Category!=UsageDataCanary)`.
`dotnet test --no-build --list-tests --filter "Category=Integration&Category!=UsageDataCanary"` (listing only, nothing run) on this tree lists **268 tests** (239 distinct names). With no filter the
assembly lists 12,198. So the forced-full push runs 268 tests of the live-connector kind in addition to the usual set, and the **slice-1 push note carries that number**, with the Linear-key warning.

### Parity and order (C6)

- **End-of-track run at CI parity:** 4 workers (`LIGHTHOUSE_TEST_WORKERS=4`), standard filter, **the lead's, on a quiet machine**, in addition to the three 6-worker runs.
- **Shuffled order:** NUnit 4 has no option to shuffle the run order. Checked in the packaged assemblies: `nunit.framework` and the NUnit3 adapter expose `RandomSeed` (for generated test arguments)
  and `[Order]`, nothing that reorders fixtures or tests. So no shuffled run is possible without editing tests. The substitute: each converted fixture is run whole, and its first-named and its
  last-named test are each run alone (`--filter "FullyQualifiedName=<Fixture>.<Test>"`); a test that depends on a predecessor's leftovers fails alone. This is recorded as a weaker check, not equivalent.
- **Green for the parallel-safety AC** means: green **except the two publish-output scans** (`NoBuildFileWritesOrReadsTheBackendPublishOutputInsideTheBackendProjectFolder`,
  `TheScanSeesTheFilesThatConsumeThePublishOutput`), which fail on unmodified `main` for an environmental reason (a root-owned `examples/postgres/data` folder). `CLAUDE.md` is not edited.

### Behaviour-preservation commands (C8)

Run after a fresh Debug build, from the repository root. The before-list is `measurement/test-ids-before.2026-11-01.txt.gz` (12,198 names, sorted, duplicates kept, 11,971 distinct; names only,
because `--list-tests` prints names without classes). Everything below is also in `measurement/scripts/behaviour-preserved.sh <base-ref>` (exit 1 if any check finds something).

```
# 1. test ids
(cd Lighthouse.Backend && dotnet test --no-build --list-tests) | tail -n +5 | sed 's/^    //' | sort > /tmp/test-ids-after.txt
zcat docs/feature/story-6460-t2-backend-host-per-fixture/measurement/test-ids-before.2026-11-01.txt.gz | diff - /tmp/test-ids-after.txt

# 2. added Ignore, Explicit, NonParallelizable (must print nothing)
git diff -U0 <base-ref> -- Lighthouse.Backend/Lighthouse.Backend.Tests | grep -nE '^\+[^+].*(\[Ignore|\[Explicit|Assert\.Ignore|NonParallelizable)'

# 3. assertion lines per file may not drop
for f in $(git diff --name-only --diff-filter=M <base-ref> -- Lighthouse.Backend/Lighthouse.Backend.Tests | grep '\.cs$'); do
  b=$(git show <base-ref>:$f | grep -c 'Assert\.'); a=$(grep -c 'Assert\.' $f); [ "$a" -lt "$b" ] && echo "DROP $f $b -> $a"
done
```

Check 3 counts lines containing `Assert.`; a removed and re-added assertion, or one moved into a helper, shows no drop, so **a reviewer reads the diff of any converted fixture's assertions**. Per fixture,
the class-qualified id check is `dotnet test --no-build --list-tests --filter "FullyQualifiedName~<Fixture>"` before and after. Run on the unchanged tree against `b2b516258` the script prints
"same 12198 names" and "nothing to look at".

### Slice sizes (D1 to D4)

| Slice | Days | Split if it grows past 3 |
|---|---|---|
| 1. Guard, D1 measurement, first move | 3 | first move to its own slice 1b (the 9 classes) |
| 2. Template | 2 (A/B, DB-4, DB-2, per-group switch) | none |
| 3. Four biggest | 3 | 3a `TaskManager` (24 classes, owns `Slice02ConfirmedNowScenarios`), 3b the other three |
| 4. Mid-size bases | 3 | by base class |
| 5. Long tail | 1 to 3, **optional** | stop at any file |

**Slice 5 is optional.** Its deliverable is value-based: either a CI-seconds target (the long tail's ceiling, about 50 s, met per the C3 rule), or the list reduced to entries that each carry a written reason
(`body-bound`, `OnAPostgresServer`, `ChecksTheTestHostItself`, `HasOneTestOnly`, `counters are per host`). If the 25 % rule moves nothing, what ships is the second: the list reduced to reasoned entries, the
guard unchanged, and the track closes without the CI claim for that slice.

**Slices 3 to 5 are re-sized on the guard's list after slice 1's D1 measurement.** Today's figures for them (proxy seconds, class counts, days) are **superseded** at that moment.

### Remaining decisions

- **Owner (D6):** the post-push measurement is owned by **the lead (the AFK session) after each slice push; reported in the slice's DELIVER block**.
- **Titles (D7):** the guard's test names are method-style by design (an infrastructure exception to the outcome-title rule; they describe a mechanical check, not a user outcome).
- **D8:** stays taken: `ClusterSubstrateProbeRegistrationTest` builds only what it inspects (widening `ConfigureHealthChecks` to `internal`); the fallback (leave the test and list it with the reason "boots the app to inspect a registration") stays recorded.
- **Windows check (B5):** the full-suite run on Windows moves to after **slice 1** as a hold item for the maintainer, and stays a hold item after slice 2.

## Wave: DISTILL / [REF] Gate round 2 (2026-11-01, ~21:10)

Forge **approved**; Architect, Eclipse and Sentinel **conditionally approved**; **0 blockers**. DISTILL is closed.
Owed at the start of DELIVER (before slice 1's first conversion commit), none of them a design change:

- Sentinel: add a planted case reached only through a virtual call whose override starts a host, and breakage rows
  for `OverridesOf` (return empty) and the no-host memo (never cache); reword the table heading to list exactly what
  was broken. Delete the never-read step constants in the planted chain; correct the newcomer stack line.
- Eclipse: DoR row 6 cites the slice-sizes table; traceability says G4 to G9; the brief's "95 files" points at the
  reconciliation.
- Architect: strike D3's original "closes over the fixture-owned object" sentence, now contradicted by the per-scope rule.
- Forge: a per-test memory trace across one converted fixture in slice 1's checks; re-measure main's peak if the base
  moved since `b573b1924`; each slice lowers `MostTypesTheListMayHold`; `behaviour-preserved.sh` writes to a per-run
  temp path; DoR "16 passed" becomes 17 (19 with the parallelization guard).

## Wave: DELIVER / [REF] Roadmap notes

Rationale and implementation detail for `deliver/roadmap.json`, keyed by step id (the roadmap keeps step descriptions short). Rigor `standard` was agreed
by the maintainer on 2026-11-01 for T1, T2 and FE7. The owed gate-round-2 items are committed (`53e6d4686`). The maintainer chose one push per slice, so slice 1's
forced-full CI run (Program.cs edit, 268 extra live-connector tests, Linear key shared with CI) is accepted.

**Conversion pattern (all conversion steps).** Remove the converted types' entries from `TypesAllowedToBuildAHostPerTest` first (newcomer test goes RED naming them),
convert (host in `[OneTimeSetUp]`, disposed in `[OneTimeTearDown]` before database files are deleted; per-test inputs reset in `[SetUp]` through fixture-owned objects; the
database file name is read per scope so `UseFreshDatabase()` is seen only by scopes created after it; nothing resolves `LighthouseAppContext` or its options from the root
provider), then lower `MostTypesTheListMayHold` in the same commit. An entry left behind after a conversion is the opposite RED (stale test). A fixture that cannot be made
safe stays listed with a written reason; it is never excused. State leaking across tests of a shared host shows as a flake in the run-alone-3-times check.
Code rules: comments rare and plain, no internal ids; no nWave scenario tags in code or test names; `dotnet build` zero warnings; git commands prefixed `LEAN_CTX_DISABLED=1`.

**Id check where tests are added.** Steps that add tests use an addition-aware check: sorted ids before are a subset of ids after, and the additions are exactly the named new
tests. All other steps: ids equal. Steps with additions: 01-03, 02-01, 02-02, 03-01, 04-04, 04-06, and the lead steps 01-10, 02-05, 03-07, 04-07.

**01-01.** Extend, no new class beyond the enum member. Contains nothing else, so 01-02 measures the tree before any change. Override is bounded-change (reads a stopwatch only).
**01-02.** No conversion commit is made before this run. Also records main's peak memory (re-measure if the base moved since `b573b1924`) and the disk high-water mark of `IntegrationTests_*.db*`.
**01-03.** Fixtures that move the host's own clock: `NamedDependencyLinkAcceptanceTest` and `Slice02ConfirmedNowScenarios` only (grep recorded in D3). Neither converts here.
**01-04.** Precedents: `ReportsAcceptanceTest.StartTheHost`, `RefinementAcceptanceTest`, the read-API fixtures; no shared default base. Subclasses whose only host start was the base's `[SetUp]` leave the list here.
**01-05.** `NamedDependencyLinkAcceptanceTest` (Slice01 to Slice04 in the same folder) is not part of this family; it is converted in 04-05.
**01-06.** `Slice01FieldChangeUpdatesItsOwners` has the largest group-A teardown (2.4 s): check files go once at fixture end after the host is disposed.
**01-07.** The only product-side edit of the track; mutation N/A (no access-modifier mutator, body untouched). Slice 1's push runs the forced-full filter.
**01-08.** Written after the first conversions so the entry says what was learnt. ARCHITECTURE.md and ADR-270: no change. No release notes.
**01-09.** Expected to be a no-op: every conversion step already lowers the ceiling, so the likely report is "already exact, N entries". The dictionary as committed is the truth; the workspace quotes the reason counts as 117/5/3/1 in one place and 120/3/3 in another.
**01-10.** Slice 1 is pushed as ONE unit: `827ab7eb8`, `e795f94c8`, `95a46eca5`, `5c095942a`, `53e6d4686` and every 01-xx commit. Records "IL-guard noise fallback: not triggered" (or triggered, with the reason and the source-scan fallback used).
**02-01.** Rejected (do not reopen): template per host, in-memory SQLite (FK, WAL, file semantics differ), EF InMemory. Template logic is test code, not mutated; its proof is these tests and the A/B.
**02-02.** Reuse the factory's delete-retry loop. The switch exists only so the A/B compares both ways in the same code and so the template can ship per group.
**02-03 branches.** (Pass) at least 30 % fall in set-up mean in a group: continue 02-04, 02-05 for the passing groups. (Fail in both groups) revert the 02-01 and 02-02 commits with `git revert` so no dead switch ships, record the A/B numbers and the failed prediction here, slice 2 ends with NO push, and slice 3's first step depends on 02-03. Seeded variant is considered only if both groups pass.
**02-04.** Find the migration and health fixtures and every direct caller of `EnsureCreated` or `Migrate` first. Opt-outs need a written reason.
**03-01.** TaskManager base `Init()` builds `RootFactory`; derived hosts per distinct configuration built in `[OneTimeSetUp]`. Clock-isolation pair named to run in order: one advances the clock, the next asserts `TestToday.FrozenNow`. Reaches `Slice02ConfirmedNowScenarios` through `SkipUnchangedForecastsAcceptanceTest`.
**03-02.** Partials `Slice02SeeWhatIsRunningTest`, `Slice04StopARefreshTest`, `Slice05AnyBrokenCredentialSaysSoTest`, `Slice06TheWarningsWithoutTheLogTest`, `Slice08HealthThatChecksItselfTest` derive from `RootFactory.WithWebHostBuilder`. Re-sized after 01-02.
**03-03.** The 53 s proxy is a ceiling (its cheapest test is a real scenario).
**03-04.** Same `LongRunning` pattern as `CacheTest`; the 60 s guard stays; check with `DOTNET_PROCESSOR_COUNT=4` beside another class. Flake cure, not a saving.
**03-06.** Captured events are fixture-owned and reset in `[SetUp]`. `UsageDataEventCeilingTests` is not here (it tests a rate limit; 04-06).
**03-07 / 04-07.** Re-size the next slice on the guard's list and the updated measurement before its first step starts.
**04-04.** The SQL-command counter is the reason an earlier story left this fixture alone; the new pair adds tests, so the id check is addition-aware.
**04-05 (mandatory).** The families from "The 34 types the classifier did not see": Blackout forecast bases (8, base `StartApplicationWithDeterministicForecast()`), Viewer embed (`S9_EmbedCookiePolicyTests`, `S13_ViewerEmbedHandshakeTests`, `S14_ViewerEmbedSubjectValidatorTests`, `S15_EmbedSurfaceOptInTests`; `S9b_EmbedCookieConcurrencyTests` goes to 04-06), Named dependency link (4, advances the host clock: carries the clock-isolation proof), Parent placement (3, lazy `TheRunningLighthouse()`), Parent from issue links (1, `Slice03AmbiguityRefusalTest`). Each is converted or stays listed with a written reason; no `NotMovedYet` entry may remain for them. The six integration fixtures `ForwardedHeaders`, `HealthCheck`, `Shutdown`, `JwtBearerAuth`, `McpInboundAuth` and `MetricsEndpoint` (`*IntegrationTest`) are listed as `NotMovedYet` today, not with a reason, so 04-05 owns them too: each is converted, or relabelled with a written reason (for example `ChecksTheTestHostItself` where it configures the host per test). The Task manager partials are 03-02.
**04-06.** Rate-limit fixtures: `S6_RateLimitingTests`, `UsageDataConsentRateLimitTests`, `UsageDataEventCeilingTests`, `S9b_EmbedCookieConcurrencyTests` (through `ViewerEmbedTestHost`, so it follows 04-05's viewer embed work). Rule: ONE derived host per configuration built in `[OneTimeSetUp]` (never a derived host per test), and rate-limiter partitions are reset only by choosing a configuration whose counters cannot collide across the fixture's tests; otherwise the fixture stays listed with the reason "counters are per host". Proof for DB-3 (the second test must not hit the first test's limit) is the converted fixtures' own tests, plus one named test pair per converted fixture if the fixture has a limit assertion.
**05-01 to 05-03.** Optional. If the maintainer declines slice 5, the phase and its three steps drop and the total falls by 3. Rule: a file whose measured host-build share is under 25 % of class time stays listed with "body-bound: ...".
**Lead checks (01-10, 02-05, 03-07, 04-07, 05-03).** `behaviour-preserved.sh <base-ref>`; run-alone-3-times plus first-named and last-named test alone for every converted fixture; recorded root-provider grep prints nothing; `ls` shows no `IntegrationTests_*.db*`; guard filter green under Debug and `-c Release` after a fresh build of each (same offender list); one full standard-filter run at 6 workers green apart from the two known publish-output scans and under the 4.73 GB stop threshold; evidence block; then push and start the CI after-measurement (n = 7, decision rule as fixed in DEVOPS and Gate revision 1 C3).

### Roadmap review (2026-11-02)

Reviewer (Haiku, standard profile): round 1 **needs_revision**, 5 blockers (B1 id check not addition-aware, B2 21 types unowned unless
optional slice 5 ran, B3 A/B failure path dangling, B4 owed items unverified, B5 descriptions too long). B4 was already done
(53e6d4686); the rest were revised in one pass. Round 2: one blocker left (04-05 adds tests but used the equal-ids check), fixed by the
lead together with NOTE N-a (01-08 depends on the slice-1 conversions) and N-e (slice 3 is pushed only after slice 2 is pushed or recorded
as reverted). NOTE N-b showed that the six `*IntegrationTest` host fixtures are `NotMovedYet`, not reasoned; 04-05 now owns them.
Carried NOTEs: 01-10 has six criteria; 03-03, 03-06 and 04-01..04-03 are coarse and are split before they start. **Roadmap approved.**

## Wave: DELIVER / [REF] 01-02 D1 measurement (2026-11-02, lead, quiet machine)

Tree `2d98a137a` (host-build timing on, nothing converted), Debug, `LIGHTHOUSE_TEST_WORKERS=6`, standard filter, T3 sampler.
Raw files: `measurement/d1-2026-11-02/` (`fixture-setup-timings.csv`, `d1-summary.txt`, `d1-by-listed-owner.csv`, `d1-full.log`).

- **Run:** 400 s wall; 11,916 passed, 2 failed (the two publish-output scans, environmental), 1 skipped.
- **Memory:** peak **4,524 MB**; min available 17,895 MB. The base moved since `b573b1924` (4,300 MB), so this is main's
  peak today and the **new baseline**. Stop threshold +10 %: **4,976 MB**.
- **Host builds:** 2,089 in the run, **282 s summed** (mean 135 ms); 309 timed fixtures. Every type that builds more than one
  host is on the allow-list or derives from a listed base, except `S2_ConnectionListPayloadShapeTests` (2 builds, 0.2 s).
- **Reachable by one host per concrete fixture, host build only: 241 s summed** across 120 listed owners, roughly **40 s wall**
  at 6 workers (about 10 % of the run). This counts host construction only. Per-test database creation, seeding and teardown
  are separate and slice 2's template is aimed at them.

| Group (roadmap) | Owners | Host-build saving, summed |
|---|---|---|
| Slice 1: ConnectionDefaults family + ConnectionFieldChange | 2 | 16.1 s |
| Slice 3: UsageDataCollectorObservation, FasterUpdates, TaskManager, ReconstructOverTimeHistory | 4 | 59.0 s |
| Slice 4: ForecastRealityCheck, BehaviourSettings, TeamFeatureSize, BlockedItems, PercentilesOverTime, ManualSorting, DeliverySources, Dependencies, FastWithManyFeatures | 9 | 59.4 s |
| Rest (single fixtures; long tail, 04-05/04-06 families included) | 105 | about 106 s |

**Re-sizing decision (lead):** the order stands. The biggest single owner, `UsageDataCollectorObservationTest`, saves 36 s,
14 classes and 279 host builds; it is already in slice 3. Slice 1 stays as planned because it carries the guard. Its own
host-build saving is small (16 s summed, about 3 s wall), so slice 1's CI claim is now **at most about 30 s CI** (16 s × 1.8),
lower than the 65 s ceiling in Gate revision C1. The pre-registered C3 rule is applied to that figure, not to the old one.
The long tail holds as much as slices 3 and 4 together, but spread over about 100 owners, each 0.1 to 6.5 s.
Slice 5 stays optional. The first conversions are S8_EmbedEntryPoint (6.5 s), DeliveryMetricSnapshotRecordingHandler
(4.7 s), WorkItemEstimateApi (3.7 s) and Slice03AzureDevOpsRelationTypeAccepted (3.7 s).
**Honest read:** host construction is about 40 % of all fixture set-up and teardown time measured. The story's total win depends
as much on slice 2's database template as on the moves.

## Wave: DELIVER / [REF] Slice 1 review (2026-11-02)

- **Verdict:** conditionally_approved, 0 blockers.
- **Conditions cleared by the lead:** (1) no assertion line changed in the two converted bases (checked by diff 5c095942a..58703b04a); (2) the cluster probe test asserted registration and tags only before the move, and still asserts the same three things.
- **Fixed in this revision:** `TheQueueGoesIdle` in both converted bases now fails loudly when the update queue has not gone idle by the deadline; the connection-defaults base resets the licence mock before arming it, like the field-change base; the clock and host-build comments in `TestWebApplicationFactory.cs` say what is true, and `ResettableFakeTimeProvider` documents that Reset shifts reported time only; the "Host lifecycles" entry in `docs/ci-learnings.md` names the reasons that exist, the guard's reach, and the singleton-state check.
- **Carried notes:** the guard does not follow interface dispatch (no fixture relies on it). The singleton maps `UpdateQueueService` awaiters/pendingReruns/heldUpdates and `OverTimeHistoryFiller.alreadyAsked` must be checked before slice 3/4 converts a chart-reading or history-fill fixture (checklist item for 03-xx and 04-xx). Timers are not rewound by Reset. The timing hook counts builds through derived factories (the 01-01 spot check showed 15 HostBuild rows for one ConnectionFieldChange fixture).

## Wave: DELIVER / [REF] 01-10 Slice 1 pre-push evidence (2026-11-02, lead)

- **Full standard-filter run** on `58703b04a`, 6 workers, quiet machine: 403 s; 11,917 passed, 2 failed (the two publish-output
  scans, environmental), 1 skipped. **Peak 4,312 MB** (baseline 4,524 MB, stop threshold 4,976 MB): under the threshold.
  The review revision after it (`90b847505`: queue drain fails loud, licence reset) re-ran the converted folders green (216/216).
- **`behaviour-preserved.sh 827ab7eb8^`** on `99e4dcd2b`: check 2 (added Ignore/Explicit/NonParallelizable) and check 3 (assertion
  lines per file) print nothing. Check 1 lists only the two named additions (`TheHostReadsTheFactoryClockAndResettingItRestoresTheFrozenInstant`,
  the planted virtual-call case) and the two cases of `What_a_created_Delivery_holds_is_settled_by_how_its_Features_were_chosen`, whose
  names embed the run's date (12/1 vs 12/2) and change every day. Not a behaviour change; same-day lists are equal.
- **Guard parity**: Release build 0 warnings; guard + parallelization guard 20/20 in `-c Release` (Debug green in every step).
- **Memory trace, one converted fixture family**: `ConfigureOncePerConnection` (161 tests, one host per fixture) peaks at 1,172 MB
  over 30 s; nothing left in `IntegrationTests_*.db*` afterwards.
- **IL-guard noise fallback: not triggered** (no false offender reported in slice 1).
- **Allow-list**: 126 -> 123 entries, ceiling 123.
- **Frontend gates**: not run; slice 1 changes no frontend or E2E file.
- **Push unit**: 827ab7eb8, e795f94c8, 95a46eca5, 5c095942a, 53e6d4686, ac6244a4b, 2d98a137a, cb5a79263, ecbfbc233, 7c4194f2f,
  1fe43f201, 4bcfa70a9, bc6ba1aca, 58703b04a, 90b847505, 99e4dcd2b and this record. `Program.cs` changed, so CI runs the forced-full
  filter (268 live-connector tests in addition; the Linear key is shared with CI).

## Wave: DELIVER / [REF] Maintainer decision: slice 5 is mandatory (2026-11-02)

The maintainer made slice 5 mandatory ("Yeah do slice 5 mandatory"). This supersedes "Slice 5 is optional" in Slice sizes
(D1 to D4) and the 04-07 decision point. The track ends only when every allow-list entry is either converted or carries a
written reason (`ChecksTheTestHostItself`, `OnAPostgresServer`, `HasOneTestOnly`, or a new reason constant with its why);
no `NotMovedYet` entry remains. Leaving the host entirely, where a test does not need it, is a separate item (#6469).

## Wave: DELIVER / [REF] 02-03 Template A/B (2026-11-02, lead, quiet machine)

Tree `7e442a1af` + a temporary, uncommitted environment switch (reverted). 6 workers, timing on, 5 runs per arm, arms
interleaved. Raw: `measurement/ab-2026-11-02/` (`summary.txt`, per-run CSV and logs). Per-test set-up mean (ms) per run:

| Fixture (group) | Template off | Template on | Median off -> on | Fall | Verdict (30 %) |
|---|---|---|---|---|---|
| `ConfigureOncePerConnection`, 161 tests (B: host per fixture, database + seed per test) | 281.7, 281.2, 275.8, 278.4, 277.9 | 213.3, 207.0, 210.4, 207.9, 213.9 | 278.4 -> 210.4 | 24.4 % | **FAIL** |
| `CsvWorkTrackingConnectorTest`, 60 tests (C: `IntegrationTestBase`, database per test) | 53.1, 65.6, 45.3, 48.9, 35.5 | 4.0, 4.0, 4.2, 4.4, 4.1 | 48.9 -> 4.1 | 91.7 % | **PASS** |

Wall time per run: group B 27 s either way (set-up is not on its critical path at 6 workers); group C 2-4 s -> 0.43-0.45 s.
All 20 runs green. **Decision (pre-registered rule):** the template ships for group C only, the `IntegrationTestBase` fixtures
(02-04). Group B keeps `EnsureCreated`; its seeding, not the schema, is the cost. **Seeded variant (D4b): not pursued**, the rule
allowed it only if both groups passed. Slice 2 is pushed (02-05).
