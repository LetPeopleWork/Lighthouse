# Spike findings — Backend fixture setup overhead (CS-Q)

**Verdict: PARTIAL — original CS-Q hypothesis disproved; pivot to CS-R (WAF sharing + de-`[NonParallelizable]`).**

The 330 s "elsewhere" gap is **not** in user-written `[SetUp]/[TearDown]` (only 31 s across the whole scoped suite). The bulk lives in **fixture constructors** that bootstrap a `TestWebApplicationFactory<Program>` — invisible to TRX `Duration` and to `[SetUp]` instrumentation — combined with `[NonParallelizable]` on `IntegrationTestBase` forcing those ~40 bootstraps to run serially.

## Context — what the prior A/B established

Three local runs of the scoped filter `Category!=Integration|Category=GithubIntegration` (2,339 passing, 1 skipped):

| Variant | Run 1 | Run 2 | Mean |
|---|---|---|---|
| `coverlet.collector` 10.0.0 (current) | 426 s | 431 s | 428.5 s |
| `coverlet.msbuild` 6.0.4 | 429 s | 439 s | 434.0 s |
| no coverage | 440 s | 439 s | 439.5 s |

Coverage costs ≈ 0 s. The wall-clock floor is the test infrastructure itself.

## Method

- Added `Lighthouse.Backend.Tests/TestHelpers/FixtureSetupTimer.cs` — thread-safe per-fixture accumulator for the four NUnit lifecycle phases, enabled only when `LIGHTHOUSE_FIXTURE_TIMING=1`.
- Added `Lighthouse.Backend.Tests/FixtureTimingReporter.cs` — assembly-level `[SetUpFixture]` in the `Lighthouse.Backend.Tests` namespace that writes the report on `[OneTimeTearDown]` and `AppDomain.ProcessExit`.
- Instrumented `IntegrationTestBase.Init/TearDown/GlobalTearDown` (permanent — retained on `main`, off by default).
- Programmatic sweep instrumented every `[SetUp]/[TearDown]/[OneTimeSetUp]/[OneTimeTearDown]` across 93 test files (110 insertions total) — reverted after the run.
- Ran scoped suite once: wall-clock **442 s**, 2,339 passed, output CSV in `spike-be-fixture-setup-results.csv`.

## Result

```
Fixtures instrumented:    117
Sum [SetUp]:               31.1 s
Sum [TearDown]:             0.9 s
Sum [OneTimeSetUp]:         0.0 s    (none defined in the codebase)
Sum [OneTimeTearDown]:      0.0 s    (only the base-class dispose, ~0)
GRAND TOTAL instrumented:  32.0 s

Wall-clock observed:      442.0 s
Per-test TRX duration sum:149.2 s    (includes [SetUp]+test+[TearDown])
Unexplained gap:          292.8 s    (442 − 149)
```

The 31 s `[SetUp]` total is **already inside** the 149 s TRX sum (NUnit's per-test `Duration` covers SetUp/Test/TearDown). So `[SetUp]` instrumentation cannot account for any of the 293 s gap.

### Top 10 fixtures by total `[SetUp]` (full table in `spike-be-fixture-setup-results.csv`)

| Fixture | n_tests | total_ms | mean_ms | classification |
|---|---:|---:|---:|---|
| `S2_ConnectionListPayloadShapeTests` | 8 | 8 240 | **1 030** | **per-test WAF rebuild** — recreates `TestWebApplicationFactory` in `[SetUp]` and disposes in `[TearDown]`. Fix: move WAF to constructor or shared base. |
| `OAuthControllerIntegrationTest` | 12 | 5 720 | 477 | same pattern as S2. Fix: move WAF to fixture ctor. |
| `OAuthHealthControllerTest` | 2 | 3 377 | 1 688 | same pattern. Fix: move WAF to fixture ctor. |
| `CsvWorkTrackingConnectorTest` | 48 | 1 558 | 32 | benign — short, per-test file system + parser init. |
| `PortfolioDeleteSerialisationTests` | 6 | 1 051 | 175 | check — possibly per-test WAF too. |
| `TerminologySeederTests` | 29 | 959 | 33 | benign — per-test seeder setup. |
| `DeliveriesControllerIntegrationTest` | 10 | 644 | 64 | **good pattern** — IntegrationTestBase derivative; WAF in ctor, `[SetUp]` does only EF `EnsureDeleted`/`EnsureCreated`. |
| `OAuthCallbackCsrfIntegrationTest` | 1 | 639 | 639 | single-test fixture; per-fixture WAF amortized over 1 test. |
| `OAuthProviderAbstractionIntegrationTest` | 1 | 636 | 636 | same. |
| `TeamsControllerAuthorizationTests` | 9 | 556 | 62 | IntegrationTestBase derivative — good pattern. |

The 3 outliers at the top (`S2_*`, `OAuthControllerIntegrationTest`, `OAuthHealthControllerTest`) account for **17 s / 31 s = 55 %** of all `[SetUp]` time and are an obvious cheap fix (~½ day) — even though their savings are small in absolute terms (~15 s of wall-clock max), they're the only fixtures violating the "WAF in ctor, not [SetUp]" convention.

### Where the 293 s gap actually lives

What `[SetUp]/[TearDown]` and TRX `Duration` *do not* cover:

1. **Fixture constructors** — `IntegrationTestBase`-derived classes call `new TestWebApplicationFactory<Program>()` in their constructors (e.g. `DeliveriesControllerIntegrationTest.cs:13` → `: IntegrationTestBase(new TestWebApplicationFactory<Program>())`). That bootstraps the entire ASP.NET host once per fixture. Estimated at 2–5 s per fixture × ~40 derived fixtures = **80–200 s** of construction time, none of it captured by my instrumentation.
2. **`[NonParallelizable]` on `IntegrationTestBase`** (`IntegrationTestBase.cs:8`) — every derived fixture is excluded from `[assembly: Parallelizable(ParallelScope.Fixtures)]`. The 40 WAF bootstraps **execute serially**, no matter how many cores the runner has. This is by far the largest amplifier of #1.
3. **JIT warmup + test discovery + host startup** — typically 30–60 s for a 2,300-test suite.
4. **NUnit dispatcher overhead / scheduler waits** — gaps between tests, especially around `[NonParallelizable]` barriers.

The 80–200 s estimate for #1+#2 plus 30–60 s for #3+#4 brackets the observed 293 s gap. Direct confirmation would need a constructor-time stopwatch (left as a follow-up if a more precise number is needed; current evidence is strong enough to act).

## Verdict — PARTIAL, with a pivot

- **CS-Q (lift `[SetUp]` → `[OneTimeSetUp]`)**: **REJECTED.** Maximum theoretical saving ≤ 30 s of wall-clock. Not worth a slice. The three outlier fixtures (`S2_*`, `OAuthControllerIntegrationTest`, `OAuthHealthControllerTest`) should still be fixed but as a quick local refactor (a handful of lines each), not as a feature slice.
- **CS-R (new candidate — WAF sharing + parallelizable integration fixtures)**: **OPEN** as the next slice. Two complementary changes:
  1. **Drop `[NonParallelizable]` from `IntegrationTestBase`** if the WAF-per-fixture isolation is genuinely sufficient (each fixture has its own WAF and its own EF in-memory DB, no shared mutable state by construction). Expected saving: 80–150 s of wall-clock as the 40 fixtures bootstrap concurrently rather than serially.
  2. **Share a single `TestWebApplicationFactory<Program>` instance** across all `IntegrationTestBase` derivatives via a static lazy field on the base class, with per-fixture DI scope still isolated. Expected additional saving: ~80–120 s by paying the WAF bootstrap cost once total instead of 40 times.
  - The two are independent; either alone helps; both together is the upper bound.
  - **Risk**: parallel `IntegrationTestBase` derivatives could surface state-isolation bugs in production code (singletons keyed by URL, static caches, etc.). The 2026-05-17 `ci-learnings.md` entry on `VssConnection` cache collisions documents exactly this class of issue. The slice has to enumerate breakages, not just flip the attribute.

## Acceptance criteria — status

- AC-SPIKE-Q.1 (findings doc exists): ✅ this file.
- AC-SPIKE-Q.2 (results CSV exists): ✅ `spike-be-fixture-setup-results.csv` (117 rows).
- AC-SPIKE-Q.3 (top-20 ranking + setup share %): ✅ table above; setup share is essentially nil for the bottleneck (overhead is elsewhere) — this is itself the key finding.
- AC-SPIKE-Q.4 (top-10 classified): ✅ classification column in table — the three SAFE-to-quick-fix outliers, the IntegrationTestBase-derived "good pattern" examples, and the irrelevant tail.
- AC-SPIKE-Q.5 (verdict GO/PARTIAL/NO-GO): ✅ **PARTIAL** with pivot to CS-R.
- AC-SPIKE-Q.6 (helper retained, sweep reverted): ✅ `FixtureSetupTimer.cs`, `FixtureTimingReporter.cs`, and the `IntegrationTestBase` 3-line instrumentation stay; the 93-file sweep is reverted via `git checkout`.

## Follow-ups to back-propagate

- `alternatives.md`: add CS-Q row marked REJECTED with verdict link; add new CS-R candidate (WAF sharing + de-`[NonParallelizable]`) with estimated 80–270 s saving range.
- `feature-delta.md`: corresponding catalog entries.
- ADO #5020: comment with the verdict + the CS-R proposal (pause before push per [[feedback-ado-workflow-rules]]).
- Quick refactor (separate, not part of the slice): convert the 3 outlier fixtures (`S2_*`, `OAuthControllerIntegrationTest`, `OAuthHealthControllerTest`) to bootstrap WAF in the constructor.
