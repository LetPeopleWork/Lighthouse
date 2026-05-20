# Spike — Backend fixture setup overhead profile (CS-Q)

**Goal (one sentence)**: Quantify how much of the non-Integration BE test wall-clock is `[SetUp]` re-bootstrap cost (vs test-body cost) so the follow-up slice can convert the heaviest per-test setups to `[OneTimeSetUp]` (or shared base-class fixtures) with named targets instead of guesswork.

**Owner story**: US-02 (catalog candidate CS-Q, discovered 2026-05-20 after the coverlet.collector / coverlet.msbuild / no-coverage A/B showed coverage is below the noise floor — see entry in [[alternatives]] once CS-Q is appended).

**Estimated effort**: ½ day.

**Learning hypothesis**:
- Confirms: Across the 44 WAF-using files (and the broader 94 `[SetUp]` users), the sum of per-test `[SetUp]` cost is ≥ 200 s of the ~440 s scoped local wall-clock (107 s test-body sum + ~330 s "elsewhere"). At least 5 fixtures are individually responsible for ≥ 10 s of cumulative `[SetUp]` time and are mechanically convertible to `[OneTimeSetUp]` (i.e. the per-test setup mutates no test-visible state). Opens `slice-be-onetime-setup` with a concrete list and an estimated wall-clock budget.
- Disproves: `[SetUp]` cost is < 100 s — the 330 s overhead lives somewhere else (test discovery, JIT warmup, parallel-scheduler waits, NUnit dispatcher contention). Rejects CS-Q; next move is profiling at the NUnit/runtime level rather than fixture level.

## Background — what the A/B actually showed

Three local runs with the scoped filter `Category!=Integration|Category=GithubIntegration` (2,339 passing, 1 skipped, ~107 s of test-body `duration_ms` from the CSV):

| Variant | Run 1 | Run 2 | Mean |
|---|---|---|---|
| `coverlet.collector` 10.0.0 (current) | 426 s | 431 s | **428.5 s** |
| `coverlet.msbuild` 6.0.4 (opencover) | 429 s | 439 s | **434.0 s** |
| **no coverage** | 440 s | 439 s | **439.5 s** |

Coverage costs ≈ 0 s; coverlet wrapper choice costs ≈ 0 s. The ~330 s gap between per-test body sum (107 s) and wall-clock (428–440 s) is **not** in the coverage pipeline. The leading hypothesis is per-test `[SetUp]` re-bootstrap — Lighthouse.Backend.Tests has **94 `[SetUp]` attributes and 0 `[OneTimeSetUp]`**; 44 test files reference `WebApplicationFactory` / `TestServer` / `CreateClient()` (cheap probe: `grep -rln "WebApplicationFactory\|CreateClient()\|TestServer" Lighthouse.Backend.Tests`).

## IN scope

On a throwaway branch (`spike/be-fixture-setup`):

- **1. Add a non-intrusive timing helper** to `Lighthouse.Backend.Tests/TestHelpers/` (new file `FixtureSetupTimer.cs`): a static `ConcurrentDictionary<string, (double setupMs, double tearMs, int n)>` that any fixture can opt into via two lines in `[SetUp]` / `[TearDown]` (`var sw = Stopwatch.StartNew()` → `FixtureSetupTimer.Record(GetType().Name, sw.Elapsed)`). Emit a final report in a one-shot `[OneTimeTearDown]` on an assembly-attached fixture (via `[SetUpFixture]` at the assembly root) — sorted descending by total setup-ms.
- **2. Sweep-instrument every `[SetUp]` attribute** in the scoped surface. Mechanical sed — `grep -rln "\[SetUp\]" Lighthouse.Backend.Tests` returns the file list; in each, wrap the setup body with the helper. Same for `[TearDown]`.
  - Skip the connector test files tagged `[Category("Integration")]` — they're out of the scoped filter anyway. The interesting files are under `API/Integration/`, `API/Security/`, `Services/Implementation/` (non-connector), `Cache/`, `BackgroundServices/`.
- **3. Run the scoped suite once locally** with the same command used in the A/B: `dotnet test -c Release --no-build --no-restore ./Lighthouse.Backend --filter "Category!=Integration|Category=GithubIntegration" --logger trx -- RunConfiguration.MaxCpuCount=0`. No coverage flags (the A/B showed they don't matter).
- **4. Capture the report**. The `[OneTimeTearDown]` writes `setup-timing.csv` (fixture, n, total_setup_ms, mean_setup_ms, total_tear_ms) into the test project root. Move it to `docs/feature/test-speed-improvements/spike-be-fixture-setup-results.csv`.
- **5. Cross-reference with the per-test CSV** (`/tmp/be-timings/26162111484/test-timings-backend.csv` or any recent successful run on `main`) — for each fixture, compute `setup_share = total_setup_ms / (total_setup_ms + sum(per-test duration_ms for tests in that fixture))`. Fixtures with `setup_share ≥ 50%` are the prime conversion targets.
- **6. Manual code-read for the top-10 fixtures**: for each, inspect what `[SetUp]` does and classify:
  - **SAFE to lift to `[OneTimeSetUp]`** — pure construction (DI container, EF InMemory context, WAF host) with no per-test mutation visible to siblings.
  - **NEEDS per-test reset** — `[SetUp]` mutates dependency state that the next test would observe (e.g. clears a cache, resets a counter); lift the heavy bootstrap to `[OneTimeSetUp]` and keep only the reset call in `[SetUp]`.
  - **CANNOT lift** — fixture sequence depends on the `[SetUp]` order or each test mutates singletons that downstream tests assert against.
- **7. Write `docs/feature/test-speed-improvements/spike-be-fixture-setup-findings.md`** with:
  - The wall-clock A/B table above for context.
  - Top-20 fixtures by total setup ms (table: fixture, n_tests, total_setup_ms, mean_setup_ms, setup_share %).
  - Per-fixture classification (SAFE / NEEDS-RESET / CANNOT) for the top 10.
  - Estimated wall-clock saving: if `n_tests` instances of `[SetUp]` collapse to 1 `[OneTimeSetUp]`, the wall-clock saving is approximately `(n_tests - 1) * mean_setup_ms` per fixture, adjusted for the parallel-fixture pool's effective width.
  - Verdict: **GO** (open `slice-be-onetime-setup` with the SAFE + NEEDS-RESET list), **PARTIAL** (only the top 3 fixtures pay off — open a narrower slice), or **NO-GO** (`[SetUp]` cost is < 100 s — overhead is elsewhere; redirect to NUnit/JIT profiling).

## OUT scope

- Applying any `[SetUp]` → `[OneTimeSetUp]` conversion. Conversions go in the follow-up slice (`slice-be-onetime-setup`), not the spike.
- Touching production code. Spike is read-only on production; only test helpers + per-fixture instrumentation, all on the spike branch.
- Integration-tagged connector tests (Jira/ADO/Linear). They're out of the scoped filter and have their own setup-cost story (CS-B spike covers that).
- Frontend test setup analysis. CS-D spike covers FE.
- Inferring fixture-setup parallelism from `[NonParallelizable]` markers — record which fixtures carry the marker in the findings doc, but the audit of whether each marker is still needed is a separate exercise.
- Removing the instrumentation tooling at the end. Keep `FixtureSetupTimer.cs` in `TestHelpers/` (opt-in, off by default via env var) so the next round of profiling is free; only revert the per-`[SetUp]` instrumentation sweep.

## Acceptance criteria

- AC-SPIKE-Q.1: `docs/feature/test-speed-improvements/spike-be-fixture-setup-findings.md` exists.
- AC-SPIKE-Q.2: `spike-be-fixture-setup-results.csv` exists with one row per instrumented fixture (fixture name, n_invocations, total_setup_ms, mean_setup_ms, total_tear_ms).
- AC-SPIKE-Q.3: Findings doc includes the top-20 setup-cost ranking and the setup-share % per fixture.
- AC-SPIKE-Q.4: Top-10 fixtures classified (SAFE / NEEDS-RESET / CANNOT) with a one-sentence justification per row.
- AC-SPIKE-Q.5: Verdict line: GO / PARTIAL / NO-GO with quantified rationale (estimated wall-clock saving, slice scope).
- AC-SPIKE-Q.6: Spike branch deleted after findings doc lands on `main`; `FixtureSetupTimer.cs` stays (opt-in via env var) for future re-runs.

## Dependencies

- A/B benchmark complete (2026-05-20) — coverlet wrapper neutralised as a possible cause; redirects effort to fixture setup.
- CS-P (`[assembly: Parallelizable(ParallelScope.Fixtures)]`) already shipped (`10903bb6`). The spike's measurements should reflect the *current* parallel reality, not a pre-parallel baseline.
- CS-G + slice-pre shipped — Cache.CacheTest's per-test stress dimension already trimmed; if Cache fixtures still appear in the top-10, that's a real residual signal.

## Reference class

NUnit test-infrastructure profiling spike. Closest sibling is `spike-cs-b-setup-split` (CS-B), which instruments Jira integration tests for setup/body split. This spike does the same exercise but across the *non-Integration* surface — different filter, same technique.

## Pre-slice SPIKE

This IS the spike. No nested spike.

## Taste tests

- Ship 4+ new components? **Borderline** — one helper class + one report + one CSV + one findings doc + an assembly-level `[SetUpFixture]` for emit. Coherent, single goal — acceptable.
- Depends on a new abstraction? **No** — `Stopwatch` + `ConcurrentDictionary` + standard NUnit attributes.
- Disproves something? **Yes** — that `[SetUp]` re-bootstrap is the 330 s overhead.
- Synthetic data only? **No** — measures real test runs.
- Identical-except-for-scale duplicate of another slice? **No.** CS-B spike covers Integration-tagged Jira; CS-Q covers the non-Integration surface.

All taste tests pass.

## Risk note

- **Instrumentation skews the measurement**. `Stopwatch` is cheap (~30 ns per Start/Stop) and per-test setup runs hundreds of times at most — overhead is well under 1 % of the measured number. Verified by the standard sanity check: run the scoped suite once *with* instrumentation off (helper present, but env var unset → noop) and once *with* it on; wall-clock difference should be < 5 s.
- **`[OneTimeSetUp]` conversions in the follow-up slice can introduce cross-test bleed**. Mitigation lives in the follow-up, not here — but flag SAFE-vs-NEEDS-RESET correctly in the findings doc so the slice picks the right pattern per fixture. If unsure, classify as NEEDS-RESET (conservative).
- **Parallel pool width affects savings projection**. A fixture's per-test setup of K ms, called N times, costs `(N-1)*K / pool_width` of wall-clock if collapsed to one `[OneTimeSetUp]`. Without knowing the effective pool width on the GitHub-hosted runner, savings projections in the findings doc should be a range, not a point estimate.

## Definition of Done

1. All AC-SPIKE-Q.* pass.
2. Findings doc back-propagates to `alternatives.md` as a new CS-Q entry under "Per-candidate scoring", and the ranking table picks up the slice-be-onetime-setup row in whatever priority the findings justify.
3. `feature-delta.md` gets a CS-Q row marked as **spike complete** with verdict.
4. ADO #5020 has a comment summarising the verdict + estimated savings (pause before `wit_update_work_item` per [[feedback-ado-workflow-rules]]).
