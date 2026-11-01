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
| 1 Problem statement | PASS | 71 test files build a whole host in `[SetUp]` or a test body (re-count below); measured set-up is 74.5 s of 170.5 s in 14 timed fixtures that do it. CI backend test step median 746 s. |
| 2 Persona | PASS | `lighthouse-maintainer` waiting on CI; parallel local sessions. |
| 3 Domain examples | PASS | `BlockedItemsAcceptanceTest`, `ConnectionDefaultsAcceptanceTest` (4 of the 5 most set-up-bound timed fixtures), `ClusterSubstrateProbeRegistrationTest`, `ReconstructOverTimeHistoryAcceptanceTest`. |
| 4 UAT G/W/T | DEFERRED | Epic catalogue rule: written at each Story's DISTILL. |
| 5 AC derived from UAT | PASS | The brief's five AC. |
| 6 Right-sized | PASS | Size L; split into five slices below. |
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
| 95 files build a `TestWebApplicationFactory` host in `[SetUp]` or a test body | `git grep -l 'new TestWebApplicationFactory' -- '*.cs'` = **111** files, **138** occurrences. `git grep -l 'WithWebHostBuilder' -- '*.cs'` = 102 files, 107 lines. Classified (`python3 -I classify.py`, `summ2.py`): `new` in `[SetUp]` or a test body = **71** files; derived hosts (`WithWebHostBuilder` / `WithTestAuthentication`) in `[SetUp]` or a test body = 67 files; either = **80** files (63 `[SetUp]`-only, 18 with a test body). | **80** (brief: 95; the 15 fewer are most likely T1 and #6302 moves plus the brief counting helper-only files) |
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
| D2 | Which fixtures move first | Ranked by measured saving: (1) the `ConnectionDefaultsAcceptanceTest` family (47.6 s local set-up, 4 of the top 5 timed) + `ConnectionFieldChangeAcceptanceTest` (10.5 s), with the guard; (2) the template database (central, benefits every database-per-test fixture, no per-fixture edit); (3) `TaskManager`, `ReconstructOverTimeHistory`, `FasterUpdates`, `UsageDataCollectorObservation` (largest untimed proxies, 305 s); (4) the mid-size acceptance bases; (5) the long tail. Rule for the tail: a file whose measured host-build share is under 25 % of its class time stays on the allow-list with a reason ("body-bound: ..."). | Ordered by measured then proxy seconds, biggest first, the ordering the Story asks for. The tail is cheap per file but each move is an edit with a regression risk; below a quarter of the time it is not worth it. |
| D3 | Per-fixture host pattern | Exactly #6302 DS-Q2: host built in `[OneTimeSetUp]`, disposed in `[OneTimeTearDown]`; per-test inputs (licence answer, clock position, counters, captured calls) live in objects the fixture owns and are reset in `[SetUp]`; the DI registration closes over the **fixture-owned object**, never a per-test field. No new `[NonParallelizable]`. A fixture that keeps a per-test derived host (it flips a configuration) uses one derived host per distinct configuration, created in `[OneTimeSetUp]`, or stays on the list with the reason. | It is the locked, measured pattern with 3 precedents (Refinement, Reports, TeamFeatureSize left alone). Risk it carries, to be tested per slice: state that used to be thrown away with the host (singletons, caches, `FakeLighthouseClock`) now lives across tests; each converted fixture is run alone three times in a row before commit. |
| D4 | Cheap per-test database | **Process-wide schema template, copied per fresh database, inside `TestWebApplicationFactory`.** A `Lazy` static builds one SQLite file with `Database.EnsureCreated()` (same `UseSqlite(..., DatabaseConfigurator.ConfigureSqlite)` options, `Pooling=False`, WAL checkpointed away and no `-wal`/`-shm` left); the options lambda that today reads `databaseFileName` copies the template to that path the first time the path is used (`File.Copy`, guarded by a lock and `File.Exists`), so `UseFreshDatabase()` and the 111 files that call `EnsureCreated` themselves keep working (EnsureCreated on a file that already has tables is a no-op). Slice 2 first proves it on one group-B and one group-C fixture (set-up mean must fall by at least 30 %); only then is it the default. Seeded variant (D4b): a per-host template with the `ISeeder`s already applied, **only if** the A/B shows seeding is what remains; decided by that measurement, not now. | Feasibility checked against today's code: the factory builds a unique file name per factory (`NewDatabaseFileName`), the options lambda reads the field on every options build, `UseFreshDatabase` already swaps the name, only `TestWebApplicationFactory` creates SQLite files (Postgres tests use `SharedPostgresContainer`, **out of scope**), the model is identical for every host, so one process-wide template is valid. Central, one file, benefits ~2,500 tests with no per-fixture edit. Risks: a test that expects a database with no tables at first use (the migration/health fixtures use their own set-up and are checked in slice 2); Windows file locking (template opened read-only for the copy; the existing delete-retry loop stays). Rejected: a per-host template (N builds instead of 1, no gain over process-wide for schema); an in-memory SQLite connection (FK/WAL/file semantics differ from the tests' file databases); EF InMemory (does not enforce FKs, per ci-learnings). |
| D5 | Guard design | `Architecture/TestHostPerFixtureGuardTest`: a ratcheting allow-list, **IL-scan with Mono.Cecil** (the same dependency and style as `IlDependencies`). Roots: methods carrying `[SetUp]`, `[Test]`, `[TestCase]`, `[TestCaseSource]`, `[Theory]` in the test assembly (inherited ones included). Reachable set: the call closure through methods and constructors **declared in the test assembly** (so a helper, a `*TestHost` constructor and a base-class `[SetUp]` are followed). Sinks: `newobj TestWebApplicationFactory<>` / `WebApplicationFactory<>`, and calls to `WithWebHostBuilder` or `WithTestAuthentication`. An offender is reported as the **top-level declaring type of the root method** (a base class `[SetUp]` is one entry for all its subclasses). Two tests, both directions, as in `PostgresContainerSharingGuardTest` and `RepositoryLeakRatchetArchUnitTest`: (a) no offender outside the list: the message names the type and says "build the host in `[OneTimeSetUp]`, reset per-test inputs in `[SetUp]`"; (b) every list entry still offends, else "remove it from the list". The comparison is a small pure step driven with made-up lists, so both failure directions are proven without breaking real code. The list starts as today's exact offenders (about 80 types) and may only shrink. | Precedents: `PostgresContainerSharingGuardTest` (source scan, file-name list with reasons, stale-entry test), `BackendTestParallelizationGuardTest` (type-name list), `RepositoryLeakRatchetArchUnitTest` (pure compare step + planted lists). IL beats a source scan here because the pattern is usually in a helper that `[SetUp]` calls, and the scan must follow calls; a regex over files cannot. Key is a type name, not a file name as the brief words it: the IL has no file, and the type is what a reviewer reads. Rejected: source scan with attribute-by-indentation (the classifier above needed three kinds of guess and still left 27 "helper" files unresolved); an NUnit `[SetUp]`-time runtime hook counting factories (catches only what runs, slows every run, cannot fail at build review time). Fallback if the closure is too noisy at DELIVER (a fixture that builds a host in a lambda it hands to a base): fall back to source scan + the Postgres guard's shape, and record why. |
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

| Slice | Content | Why it stands alone | Measured / estimated saving (CI-summed) |
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

1. **Wait for at least 5 successful `main` runs** that contain the slice's commit (list them under `ci.yml`, because `ci_backend.yml` is a reusable workflow and `gh run list --workflow ci_backend.yml` is empty). Include the `main` runs of later slices only if the slice-by-slice attribution is not needed; otherwise stop the count at the next slice's push. The GitHub run-list API serves stale pages about 1 time in 7, so re-list before trusting a count.
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
- **Slice 2 adds** the one-fixture A/B (one group-B and one group-C fixture, set-up mean must fall by at least 30 %) before the template becomes the default.

## Wave: DEVOPS / [REF] Windows file-lock risk of the template copy (slice 2)

CI is Linux, so CI cannot see this. The Standalone build and Windows developers run the suite on Windows, where an open handle blocks a copy or a delete.

- **Risk:** `File.Copy` of the template while another process or thread has it open for write, or a later delete of a copied database that SQLite still holds open (the existing code already has a delete-retry loop for this).
- **Mitigation 1, unique file names:** every copy gets its own unique name (the existing `NewDatabaseFileName` scheme; the template has a fixed, separate name that no test ever opens as its database). Two tests never share a destination, so there is no destination contention.
- **Mitigation 2, copy instead of open:** the template is built once, behind a `Lazy` static and a lock, with `Pooling=False` and the WAL checkpointed away (no `-wal` or `-shm` file left), then **closed completely before any copy**. Tests only ever `File.Copy` it (read share), never open it with SQLite, so nothing holds a write handle on the source. A copy that fails with an `IOException` is retried through the same bounded retry the delete uses, then fails with a message naming the path (never skipped).
- **Check:** the maintainer or a Windows developer runs the full backend suite once on Windows after slice 2; this is the only available verification and is listed as a hold item at the end of the track, not claimed as done.

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
  eight tiny abstract types inside the guard file, skipped by the real scan. NUnit runs any class that has `[Test]` methods even when it is
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
| G8 | The scan sees a host started per test however it is reached: per-test set-up, test body, helper called from set-up, lambda, after an `await`, derived host (`WithWebHostBuilder`); and does not flag a host started only in one-time set-up, or no host | `TheScanSeesAHostStartedForEveryTestHoweverItIsReached` (8 cases) | green (8 cases) |

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

Counts: 8 guard scenarios (14 test results), plus the preservation shape repeated per slice. Failure-direction share: G1, G2, G4, G5, G7 and the
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
| Set-up share per harness measured before any change | **Lead-owned** (DESIGN measurement done for 109 timed fixtures; the untimed ones become measurable through D1 in slice 1, S1.6) |
| Hosts built per fixture, per-test inputs reset in `[SetUp]` | S1.1 to S1.4 per slice; ratchet G1, G2, G3 |
| Every backend test stays green, none deleted, skipped or weakened | S1.1, S1.3 per slice (E-names, E-weaken); AC 6 |
| Parallel safety: 3 consecutive green full runs, no new `[NonParallelizable]` | AC 7, **lead-owned run** at the end; S1.7 per slice |
| A guard stops it coming back (fails on a newcomer, fails on a stale entry, passes today) | G1 to G8 (written and live now) |
| Payoff: CI after-medians (summed seconds of moved classes, step median of at least 5 runs) | **Lead-owned** (DEVOPS measurement protocol, after each push). No test asserts a time. |
| Mutation score of touched test helpers held | N/A, because Stryker.NET mutates the product assembly only (DEVOPS); the guard's equivalent is G4 to G8 |

## Wave: DISTILL / [REF] Test placement and scaffolds

- Placement: `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostPerFixtureGuardTest.cs`. Precedent: `PostgresContainerSharingGuardTest`, `BackendTestParallelizationGuardTest`, `RepositoryLeakRatchetArchUnitTest` (both-direction tests with a list, a pure compare), `IlDependencies` for the module reader (reused; the call walk is new and lives inside the guard file as a nested class).
- **Scaffold = the finished guard.** Nothing needs a stub: the guard compiles against code that exists, is live and green on today's tree, so no `[Ignore]` is used.
- Entries are keyed by the **simple name of the top-level type** that holds the `[SetUp]` or test method (DESIGN: type name; two classes with the same simple name in different namespaces would share an entry, none do today). Three reasons are in use: `NotMovedYet` (the backlog), `OnAPostgresServer` (3 types), `ChecksTheTestHostItself` (3 types).
- How the scan works (so DELIVER can trust or fix it): roots are methods with the five NUnit attributes; calls, constructors, `ldftn` delegates and the `MoveNext` of async and iterator methods are followed inside the test assembly; a virtual call on a type written in the assembly also reaches its overrides; the factory types themselves are not walked into (they are the sink); a method whose whole closure is clean is remembered.
- Known limits (not fixed): interface calls are not resolved to their implementations; methods overloaded with the same name and argument count are treated as all of their overloads (can only over-report); a host started through reflection, or through a factory type that is not a `TestWebApplicationFactory`/`WebApplicationFactory` subclass, is not seen. The Debug assembly was scanned; DEVOPS asked for one comparison against a `-c Release` assembly before the list is final; **not done here** (only the Debug build was allowed), left for slice 1.
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
constructor called from a per-test method. The scan follows those; the classifier did not. About 29 of the 34 unseen files were not examined one by one: they
are listed because a per-test method reaches a host start. If slice 1 finds one that cannot be shown to start a host per test (for example a helper called
from `[OneTimeSetUp]` that a test also reaches), it comes off the list, with the reason written in the commit. Nothing more is claimed.
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

## Wave: DISTILL / [REF] Definition of Ready (DISTILL)

| Item | Status | Evidence |
|---|---|---|
| Reconciliation, 0 contradictions | PASS | above |
| 4 UAT G/W/T | PASS | the seven above |
| Every brief AC maps to a scenario or an owner | PASS | traceability; payoff, the 3 full runs and the set-up share are lead-owned |
| Failure-direction share | PASS | the guard is all failure-direction scenarios (G1, G2, G4, G5, G7, the negative planted cases) |
| Test placement with precedent | PASS | `Architecture/` guard fixtures |
| Scaffolds green at hand-off | PASS | build 0 warnings; 16 passed, 0 skipped; no `[Ignore]` |
| RED classified as the right reason | PASS | both directions printed above |
| No scenario tags, no internal refs in code | PASS | checked in the new file |
| Open for DELIVER | NOTE | Release-assembly comparison of the list (DEVOPS) not yet done; the Windows copy risk and D8's Program.cs push cost stay as written |
