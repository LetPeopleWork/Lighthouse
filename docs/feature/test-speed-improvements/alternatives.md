# Alternatives Memo — test-speed-improvements (Slice 02, US-02)

**Status**: data-grounded recommendation, n=1 CI run (`Build And Deploy Lighthouse` [25997480487](https://github.com/LetPeopleWork/Lighthouse/actions/runs/25997480487), `main` @ `12528ae0`, 2026-05-17).
**Author**: Lighthouse maintainer.
**Decision asked**: pick top-1 or top-2 candidate slices to open next; reject the rest with reason.

---

## Baseline

| Stack | Tests | Wall-clock | Largest contributor | Cheapest single win |
|---|---|---|---|---|
| Backend | 2 534 | **528.1 s** | 121 Integration tests (314.1 s, 59 %) | 7 `CacheTest.Concurrent*` (133.7 s, 25 %) |
| Frontend | 2 843 | **288.1 s** | Top-5 spec files (57.3 s, 20 %) | n/a — long-tail dominated |

Key observation: **the BE bottleneck is bimodal**. Two clusters together account for 84 % of BE wall-clock (Integration 314 s + Cache concurrency 134 s = 448 s of 528 s). The remaining ~2 400 unit tests run in ~80 s total. Any candidate that doesn't touch one of those two clusters is rounding error.

FE is monomodal-with-long-tail: top-5 files = 20 %, top-10 = 30 %, top-20 = 45 %, top-50 = 74 %. Average FE test = 101 ms; 95 % finish under 200 ms.

---

## Method

For each candidate the score is:

- **Hypothesis it disproves** — if the slice ships and the data still shows the bottleneck, what assumption was wrong?
- **Effort** — back-of-envelope crafter days.
- **Coverage-invariant impact** — does D5 (API coverage) survive? Does mutation-kill rate survive?
- **Expected wall-clock gain** — savings in absolute seconds **and** as a % of the relevant baseline, calibrated to the numbers above. Where the calculation depends on an assumption (e.g. "setup is 50 % of integration runtime"), I state it.
- **Recommendation** — `OPEN as slice` / `HOLD pending another candidate's outcome` / `REJECT with reason`.

Honest caveats: n=1 CI run. Cache-test timings on CI vary wildly with runner cores; the next 2-3 runs will tighten that. Integration test timings depend on real Jira/ADO latency from the GitHub-hosted runner's egress to the test tenants and may shift week-to-week.

---

## Per-candidate scoring

### CS-A — Cadence split: run `[Category("Integration")]` only on `main` + release tag

- **Disproves**: "Real-API calls dominate PR-CI wall-clock and we can pay that cost less often without losing signal."
- **Effort**: 1 day (`.runsettings` filter + new workflow YAML; rerun-on-failure semantics need thinking).
- **Coverage**: Medium risk. D5 (API-coverage invariant) survives because release-tag pipelines still exercise every API; the *cadence* of detection drops from per-PR to per-merge. Real upstream drift is caught later — fine for our release rhythm if it's at most a day, painful if a contributor's PR breaks integration and we only learn after merge.
- **Expected gain**: PR BE wall-clock 528 → 528 − 314 = **214 s (-59 %)**. Largest single saving available with no test changes.
- **Recommendation**: **HOLD behind CS-B and CS-G**. The win is huge but the cadence change is irreversible signaling-wise (contributors will start expecting "PR green ⇒ integration green" to fail). If CS-B and CS-G together meet the velocity target without changing cadence, this stays a documented fallback. If they don't, CS-A is the next slice to open.

### CS-B — Fixture session sharing in connector tests

- **Disproves**: "Most of the integration runtime is auth + connection setup, not the assertions, and per-class reuse saves the bulk of it."
- **Effort**: 2-3 days (touch ~6 connector test files; mind the 2026-05-17 cache-collision and `AuthenticationMethodKey` learnings; per-class `OneTimeSetUp` instead of per-method).
- **Coverage**: Low risk. Same API calls exercised, same assertions made — just deduplicated auth + connection setup. Mutation kill rate unchanged (the same lines are still under test).
- **Expected gain**: Depends on the setup/body split, which we don't yet measure. Heuristic: `JiraWriteBackTest` has 13 methods running 14-56 s each (median ~24 s); if setup is the documented ~5-10 s of token mint + project enumeration + epic creation per method, sharing it across the class drops the per-method cost to body-time only (5-15 s saved per method × 13 methods = **65-195 s saved**, i.e. **-12 % to -37 % of BE total**). The same pattern applies to `AzureDevOpsWriteBackTest` and `JiraWorkTrackingConnectorTest`, possibly doubling the saving.
- **Recommendation**: **OPEN as slice 03 candidate.** Highest-confidence win that preserves D5 cleanly and doesn't change cadence. The estimate range is wide (n=1) but every credible point in the range moves the needle.

### CS-C — Recorded cassettes (VCR-style replay)

- **Disproves**: "We can verify our request shape and response handling without paying real-API latency on every PR, and the re-record cadence is enough drift protection."
- **Effort**: 4-5 days (WireMock or `PollyCache`-style; cassette management + re-record CI job + first-time-record bootstrap discipline).
- **Coverage**: Medium-to-high risk. PR runs prove our code shape; real upstream drift is only caught on re-record cadence. Acceptable IF re-record is automated and gated. Real-world experience: cassette-based suites accumulate stale fixtures and the maintenance debt eventually exceeds the speed win.
- **Expected gain**: Integration 314 s → ~30-60 s of replay overhead = **~250-280 s saved (-47-53 % of BE total)**. Comparable to CS-A in raw seconds but with worse drift detection and higher maintenance cost.
- **Recommendation**: **REJECT for this cycle.** CS-A delivers similar speed with simpler maintenance (no cassette files to drift). CS-B + CS-G will likely make CS-C unnecessary. Reopen if CS-B disappoints AND the team accepts cadence-split as too coarse a signal.

### CS-D — Per-spec FE fixes (top-N slowest)

- **Disproves**: "A small handful of specs accounts for the FE bulk; standard refactor patterns (fake timers, smaller fixtures, fewer DOM mounts) win."
- **Effort**: 1-2 days, scoped per spec.
- **Coverage**: None.
- **Expected gain**: Top-5 files = 57.3 s → **-20 % of FE** if cut in half. Top-20 files = 128.8 s = **-45 % of FE** if cut in half but ~20 refactors. FE is **already** 35 % of total CI test time (288 s vs 528 s BE); spending 1-2 days for a 20 % FE win = ~57 s of CI per build saved, only 8 % of total CI test wall-clock. The marginal value vs CS-G/CS-B is low.
- **Recommendation**: **HOLD.** Worth doing eventually but BE wins are larger and cheaper. Revisit after CS-G + CS-B land — if BE drops to under 200 s the FE 288 s becomes the new bottleneck and CS-D moves up the priority list automatically.

### CS-E — Vitest config tuning probe

- **Disproves**: "FE slowness is config, not test content — workers/isolation/cold-start changes win without touching test code."
- **Effort**: ½ day (a few experiments + verification across 2-3 back-to-back runs).
- **Coverage**: None directly. Config changes can mask flake; needs back-to-back green runs to validate.
- **Expected gain**: Speculative. Best case ~10-30 % of FE = 29-86 s saved. Worst case 0 (config is already reasonable; existing `pool: "threads"`, `isolate: true`, `maxWorkers: undefined`, `fileParallelism: true` is already optimistic).
- **Recommendation**: **OPEN as a half-day spike in parallel with CS-B.** Cheap learning. If it pays off, ship it; if not, the cost was negligible.

### CS-F — Backend parallel/isolation hardening

- **Disproves**: "Static state in connectors and shared cache keys is silently capping the parallelism we already configured."
- **Effort**: 2 days (change cache key shape, add credential fingerprinting per ci-learnings 2026-05-17 entries).
- **Coverage**: None — isolation is strictly improved.
- **Expected gain**: Limited by runner capacity. `ubuntu-latest` GitHub runners are 4-core (2 vCPU pairs); with `MaxCpuCount=0` already set, the headroom is small. CS-F's value is mostly *correctness* (no flake from collisions), not speed. Real gain likely <30 s of BE wall-clock.
- **Recommendation**: **HOLD.** The static-cache issue was partially addressed in the 2026-05-17 fixes already; the residual is incremental. Reopen if CS-G + CS-B don't get us to target.

### CS-G — Cache concurrency test downscaling (NEW from Slice-01 evidence)

- **Disproves**: "A small number of intentionally-thread-heavy unit tests scale poorly on CI's limited cores and dominate BE wall-clock."
- **Effort**: ½ day. The 3 implicated tests (`ConcurrentReadersAndWriters_*`, `ConcurrentInsertsOnUniqueKeys_*`, `ConcurrentGetOnExpiredKey_*`) currently use 64-thread barriers (32 readers + 32 writers, or 64 inserters) and 1-second timed loops. On a 2-vCPU runner those threads serialize hard. Reducing to 8+8 threads and 200 ms loops keeps the concurrency invariant (still finds torn reads / lost updates if they exist — the assertion is over correctness, not throughput) while cutting cluster cost by an order of magnitude.
- **Coverage**: None. Same invariants asserted (no torn reads, no exceptions, all values retrievable). The mutation kill rate on `Cache<,>` is unchanged because the assertion surface is unchanged; only the stress dimension shrinks. The original bug-5016 regression was reproducible at much lower thread counts (it's a data-race on `ConcurrentDictionary` access, not a 64-thread-only phenomenon).
- **Expected gain**: Cache cluster 133.7 s → ~13-20 s = **~115 s saved (-22 % of BE total)** on the CacheTest fixture alone. Ratios assume linear scaling with thread count, which is conservative — actual scaling on a 2-vCPU runner is likely worse than linear, so the win may be larger.
- **Recommendation**: **OPEN as slice 03A — ship first.** Cheapest by a wide margin (½ day vs CS-B's 2-3 days), zero coverage risk, fastest individual BE win available.

---

## Combinations

### CS-G + CS-B  *(new, top recommendation)*

Ship CS-G first (½ day, BE 528 → ~415 s = **-21 %**). Then evaluate CS-B against the new 415-s baseline. Cumulative best case: BE → ~265 s = **-50 %** with no cadence change, no cassette infrastructure, no coverage regression.

**Recommended.** Lowest-risk path to the largest win.

### CS-B + CS-A

Ship CS-B (-15 to -37 %). If integration time on PR is still painful, follow with CS-A to push it off per-PR. The hand-off point is empirical: re-measure after CS-B lands.

**Hold as fallback** if CS-G + CS-B don't satisfy target velocity.

### CS-F + CS-D

Cheapest pair if both work, but both have unclear ceilings and CS-F's residual gain is low after the 2026-05-17 work. **Hold.**

### CS-C alone

**Reject.** Highest setup cost, biggest payoff if real-API latency is the dominant variable — but CS-G + CS-B + CS-A together get us most of that payoff with simpler infrastructure.

---

## Ranking — top picks to open

1. **CS-G** (cache concurrency test downscale) — open as slice-03A. **½ day, -22 % BE, zero coverage risk.** Cheapest first move.
2. **CS-B** (fixture session sharing) — open as slice-03B after CS-G lands and the new baseline is captured. **2-3 days, additional -15 to -35 % BE, low coverage risk.**

In parallel, optionally **CS-E** as a half-day spike (if it cleanly wins, ship; if not, abandon — total cost: ½ day even on the rejected outcome).

Everything else (CS-A, CS-C, CS-D, CS-F, and the other combos) stays HOLD or REJECT for this cycle with the reasons above. Reopen via a fresh DISCUSS pass if the data after slice-03A/B contradicts these assumptions.

---

## What this memo doesn't cover

- **n=1**. Numbers may shift ±20 % across runs; the ranking above tolerates that (CS-G is so much cheaper than alternatives that no plausible variance changes its #1 status). Capture 2-3 more `main` runs once CS-G lands so the CS-B effort estimate becomes empirical instead of heuristic.
- **Setup vs body split on integration tests** is *not* measured by Slice-01's TRX-level granularity. If CS-B's gain disappoints, instrument NUnit `OneTimeSetUp` timing separately before continuing.
- **FE long-tail savings** beyond CS-D are out of scope per Slice-01 (no E2E, no Playwright sharding — D6).
- **Cross-runner OS variation** (Windows/macOS verify-* jobs) — Linux-runner numbers are canonical per slice scope.

---

## Next action

Open slice-03A for CS-G under the existing ADO Story #5020 (no new child Stories per D8). Brief lives at `docs/feature/test-speed-improvements/slices/slice-03a-cache-concurrency-downscale.md` (to be written when slice-03A starts).
