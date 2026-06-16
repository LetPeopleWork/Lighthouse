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
