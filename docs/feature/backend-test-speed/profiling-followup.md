# Backend test-time profiling — follow-up opportunities (post-#5258)

Profiled 2026-06-16 on a 12-core local machine after Slices 02–04 landed. Source: a full
`dotnet test … --filter "Category!=Integration"` run with `--logger trx` (per-test durations,
`Scripts/test-timings/trx_to_csv.py`) + `LIGHTHOUSE_FIXTURE_TIMING=1` (per-fixture setup CSV).

**Read the durations as relative, not absolute.** Per-test durations are wall-clock under
`ParallelScope.Fixtures`, so they are inflated by CPU contention (sum of per-test = 480 s vs
~343 s actual wall). They rank hotspots reliably; the absolute "saving" figures below are
order-of-magnitude, and the redundant-work arguments are structural (independent of contention).

These are **out of #5258's scope** (BE-only parallelization root-cause, now delivered). They seed
a new story.

## Ranked opportunities

### #1 — Cache the ArchUnitNET `Architecture` once (highest value, lowest risk)

13 architecture/seam fixtures (29 tests total) account for **~114 s attributed** — the heaviest
per-test cost in the suite (single arch tests measure 5–10 s each). Each fixture independently
does:

```csharp
private static readonly Architecture Architecture = new ArchLoader().LoadAssemblies(...);
```

`ArchLoader.LoadAssemblies` builds the full type-dependency graph of the Lighthouse assembly — a
multi-second operation — and it runs **13 times**, once per fixture class. All 13 load the **same**
production assembly (`BaseMetricsService` / `TeamMetricsService` / `WorkItemService` /
`BlackoutDaysExtensions` are all in it); only `DomainEventDispatcherSeamArchUnitTest` adds the
`Microsoft.Extensions.DependencyInjection.Abstractions` assembly.

**Fix**: one shared `static readonly Lazy<Architecture>` (loading the Lighthouse assembly + DI
abstractions as a superset) in a common helper; every arch fixture reads it. The `Architecture`
model is immutable, so sharing it read-only across fixtures is safe under parallelism. Builds the
graph **once** instead of 13×.

**Risk**: low — pure read-only sharing, no behaviour change. **Effort**: small (~13 one-line edits
+ one helper).

### #2 — Per-fixture WebApplicationFactory reuse for the 38 `[SetUp]` builders

38 fixtures build their `TestWebApplicationFactory` in `[SetUp]` (per **test**) rather than once per
fixture (264 tests → **226 redundant host builds**). At the Slice-01 warm cost of ~187 ms/build
that is ~42 s aggregate (~10 s wall @4 cores). The real figure is likely higher: these fixtures
average ~1000 ms/test vs ~500 ms for non-WAF service tests, implying ~300–500 ms of WAF overhead
per test → potentially ~25 s wall on a 4-core CI runner.

**Fix**: build the WAF once per fixture (`[OneTimeSetUp]` / constructor, as `IntegrationTestBase`'s
default-ctor path already does) and keep only the per-test DB reset in `[SetUp]`. The Slice-02
isolation primitives (`Pooling=False`, per-test `EnsureDeleted`/`EnsureCreated`) must move to the
per-fixture host; re-verify 3× green.

**Risk**: medium — must preserve per-test DB isolation with a reused host. **Effort**: medium,
mechanical across ~38 fixtures. This is the Slice-02 deviation note, promoted.

### #3 — DB-bootstrap cost / parallelism tuning (smaller, measure after #1–#2)

The per-fixture DB bootstrap (`EnsureDeleted`+`EnsureCreated`) is ~77 ms × 264 ≈ ~20 s aggregate.
Held-open in-memory SQLite (vs file-per-fixture) would cut the file I/O — but it is a test-fidelity
change (DISCUSS marked it out of scope) and should only be considered if #1–#2 don't get CI under
target. Likewise `LevelOfParallelism` tuning only if a CI run shows core under-utilisation.

## Top fixtures by attributed time (ranking only)

| Fixture | Tests | Attributed | Cluster |
|---|---|---|---|
| CumulativeStateTimeReadApiIntegrationTest | 25 | 25.2 s | WAF/setup |
| AgeInStatePercentilesNonLinearFlowReadApiIntegrationTest | 13 | 17.0 s | WAF/setup |
| CumulativeStateTimePortfolioReadApiIntegrationTest | 15 | 14.8 s | WAF/setup |
| ForecastServiceTest | 30 | 14.6 s | (pure service) |
| FlowEfficiencyReadApiIntegrationTest | 13 | 12.6 s | WAF/setup |
| DomainEventDispatcherSeamArchUnitTest | 1 | 10.6 s | ARCH |
| LicenseGateSingleSourceArchUnitTest | 1 | 10.2 s | ARCH |
| ForecastFilterSeamArchUnitTest | 1 | 10.1 s | ARCH |
| (… 10 more arch/seam fixtures, ~8–10 s each) | | | ARCH |

**Recommendation**: open a follow-up story; do #1 first (cheap, structural, biggest per-test win),
then #2 (the larger aggregate but riskier), measure CI, and only then consider #3.

---

## Update 2026-06-16 — what shipped, the real diagnosis, and remaining levers

**Shipped into #5258 (test-only + CI config):**
- Slice-05: arch-cache (`LighthouseArchitecture.Production`) — arch cluster ~114 s → ~5 s *attributed*; **no local wall-clock change** (it overlapped other parallel work; pure CPU-work reduction, helps a core-constrained CI runner).
- Slice-06: per-fixture WAF reuse for the 11 data-driven read-API fixtures — heaviest fixture ~12 s → ~6 s; full-suite local wall-clock ~345 s → ~330 s (~5%).
- Coverage scoping (`ci_backend.yml` `Include="[Lighthouse]*"`) — coverage tax **+63% → +29%** on a representative cluster; identical Sonar numbers.

**The real diagnosis (measured 2026-06-16):** the suite is **wait-bound, not CPU-bound** — on a 12-core box it uses only **2.5–5 cores**, decaying as it ends in a long tail of heavy integration fixtures running nearly alone. So parallelization (02–04) had almost no headroom (serial baseline 336 s vs parallel 330 s) and CPU-work cuts (05) couldn't move wall-clock. The waits are **SQLite file I/O + per-test schema drop/recreate** (`DataSource=file;Pooling=False` + `EnsureDeleted`/`EnsureCreated` every test) plus in-process HTTP. CI additionally pays the coverage-instrumentation tax (inherent — coverlet opencover and the native MS collector measured equal at ~+61–63%; scoping to `[Lighthouse]*` is the only cheap reduction found).

**Remaining levers, ranked (evidence-based), for a future story — NONE YET DONE:**
1. **In-memory SQLite + schema-once** — the top *structural* lever for the wait-bound I/O. Held-open `SqliteConnection` per fixture (in-memory DB lives only while a connection is open), build schema once per fixture (`[OneTimeSetUp]`), reset between tests by truncating rows / transaction-rollback instead of `EnsureDeleted`+`EnsureCreated`. **Unproven — needs a spike on one heavy fixture to get a hard number before rollout.** Medium effort/risk (behaviour parity, connection lifetime). DISCUSS deferred it on test-fidelity grounds, but the wait-bound evidence says it's *the* lever.
2. **CI test sharding (matrix)** — split the assembly across N runners via `--filter` partitions. Cuts CI wall-clock ~linearly **and** splits the coverage tax across runners; independent of the in-process bottleneck. Most reliable CI win. Cost: runner-minutes + workflow plumbing.
3. **Split the long-tail fixtures** — tests within a fixture run serially, so the heaviest fixtures (e.g. 25-test chains) define the wall-clock floor. Splitting them into smaller fixtures lets them parallelize *as fixtures*. Simpler than intra-fixture parallelism (which fights the shared-fixture-DB model).
4. **Remaining per-`[SetUp]`-WAF builders** — Slice-06 converted only the 11 clean read-API fixtures. NOT converted: 7 mock-injecting builders (`DeliveryMetricsHistory`, `ForecastFilterThroughputChart`, `ForecastFilterTeamSettings`, `CycleTimeDefinitionSettings`, `CycleTimeDefinitionValidity`, `Portfolio/TeamStalenessThresholdSettings` — they configure the host with per-test mocks; reuse needs a per-fixture mock-invariance check) + 14 small (<5-test) builders (≤3 builds saved each — low value). Config-rebuilders (`S1_*`, `S5_*`, `F_BE_1`, `OAuth*`) must stay per-test (env/auth read at host startup).

**#5258 close-out tail (still open, on hold per user):** BaseMetricsService mutation run (D9, the only touched production file — Slice-02); ADO #5258 before/after comment (AC-04.4) + transition to `Resolved`; `nw-finalize` (archive to `docs/evolution/`, clean workspace). The optimization levers above are explicitly a *separate future story*, not #5258.
