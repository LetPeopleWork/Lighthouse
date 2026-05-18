# Alternatives Memo — test-speed-improvements (Slice 02, US-02)

**Status**: **RESUMED 2026-05-18** — data-grounded ranking calibrated against 11 BE / 15 FE runs since the Slice-01 baseline. Earlier (n=1) PAUSED state lifted; the ranking holds with the multi-run numbers, and one new candidate (CS-H) plus one labelling-bug discovery have been back-propagated to `feature-delta.md`'s catalog.
**Author**: Lighthouse maintainer.
**Decision asked**: pick the slices to open next; reject the rest with reason.

---

## Baseline (multi-run, 2026-05-18)

Across 11 backend timing artifacts and 15 frontend timing artifacts collected since 2026-05-17. The strictest filter (`main` branch, green-tests, n=4) is the headline; the broader filter (all 11 runs incl. dependabot branches) gives a noise-bound check.

### Backend (n=4 main green-tests; broader n=11 in parens)

| Cluster | Mean ± stdev | stdev / mean | Range |
|---|---|---|---|
| Total BE wall-clock | **575.8 s ± 55.6 s** (broader: 592.6 ± 46.3) | 9.7 % (broader: 7.8 %) | 528.1 – 651.4 s |
| `[Category("Integration")]` (umbrella) | **346.8 s ± 36.3 s** | 10.5 % | 314.1 – 396.7 s |
| `Cache.CacheTest.Concurrent*` | **142.2 s ± 13.4 s** (broader: 133.9 ± 25.6) | 9.4 % (broader: 19.1 %) | 128.8 – 157.0 s |
| Unit/Other | 86.9 s ± 7.4 s | 8.5 % | 80.8 – 97.7 s |

**Cluster-level decomposition** (median per connector across all 11 BE runs, *after counting the mislabelled tests — see "Labelling-bug discovery" below*):

| Connector | Time (median) | % of Integration cluster | Files |
|---|---|---|---|
| **Jira** | **350.0 s** | **88 %** | `JiraWriteBackTest`, `JiraScopedTokenIntegrationTest`, `JiraWorkTrackingConnectorTest` |
| Azure DevOps | 37.9 s | 10 % | `AzureDevOpsWriteBackTest` (tagged) + `AzureDevOpsWorkTrackingConnectorTest` (**mistagged, currently in Unit/Other**) |
| Linear | 8.9 s | 2 % | `LinearWorkTrackingConnectorTest` (**mistagged, currently in Unit/Other**) |
| GitHub / other | 2.4 s | < 1 % | `LighthouseReleaseServiceIntegrationTest`, `GithubServiceTest` |
| **True Integration total** | **~397 s** | 100 % | — |

> **Labelling-bug discovery (back-prop to feature-delta.md)**: `AzureDevOpsWorkTrackingConnectorTest.cs` (85 methods, mean 29.5 s) and `LinearWorkTrackingConnectorTest.cs` (19 methods, mean 8.9 s) both read real-API tokens from env vars but have no `[Category("Integration")]` attribute. They currently count toward the Unit/Other cluster. Fixing the labelling is now a prerequisite slice (slice-pre-integration-labelling); it does not move BE wall-clock by itself, but it unblocks CS-H and makes `dotnet test --filter "Category!=Integration"` truthful for local dev.

### Frontend (n=5 main green-tests; broader n=15 in parens)

- Total FE wall-clock: **305.2 s ± 20.4 s** (6.7 % stdev) — broader 293.0 s ± 30.3 s (10.4 %). FE is rock-stable across runners.
- Top-3 files account for **42 s of the 305 s** (14 %) — see "Per-candidate scoring → CS-D refined" below.
- Top-10 = 30 %, top-20 = 45 %.

### Empirical commit-pattern weighting (added for CS-H scoring)

Across 585 non-merge commits since 2026-04-01:

| Touched path | Count | % | Resolver action under CS-H |
|---|---|---|---|
| Jira-only | 3 | 0.5 % | run Jira (~350 s) |
| ADO-only | 4 | 0.7 % | run ADO (~38 s) |
| Base / both / CI config | 61 | 10.4 % | run all (~397 s) |
| Neither | **517** | **88.4 %** | **skip all (0 s)** |

Weighted-average integration time per PR under CS-H = 39 s, vs ~361 s today = **−322 s saved per average PR = −56 % of BE wall-clock on the 88 % of PRs that don't touch connectors.**

### Key observations vs n=1 memo

1. **All clusters' stdev is well under the 30 % resume threshold.** Strictest filter is 5–10 %; broadest is 8–19 %. Numbers can be trusted.
2. **`ConcurrentReadersAndWriters_DoNotObserveCorruptedEntries` is *the* test**: median 115.8 s, range 65.2 – 128.6 s. Accounts for ~85 % of the Cache cluster on its own — CS-G's primary target.
3. **Cache.Concurrent has the highest cluster noise (9 % main, 19 % broader)** *because* it's CPU-contention-sensitive on 2-vCPU runners. Three dependabot-branch runs hit 90–103 s on cache; main runs hit 128–157 s. Same code, different runner tier ⇒ ±40 % variance. CS-G addresses this directly: smaller stress dimension ⇒ smaller variance ⇒ tighter signal.
4. **The Integration cluster is 88 % Jira.** ADO and Linear are minnows once you correct the labelling. CS-A "cadence-split everything" is overkill; CS-H "scope it to what the PR touched" is sharper and protects detection latency.
5. **No new candidate emerged from the multi-run data** *other than* CS-H — and CS-H was prompted by user feedback during the resume, not by a hidden pattern in the numbers.

---

## Method (unchanged from n=1 version, retained for reference)

For each candidate the score is:

- **Hypothesis it disproves**.
- **Effort** — back-of-envelope crafter days.
- **Coverage-invariant impact** — D5 (API coverage), mutation-kill rate.
- **Expected wall-clock gain** — absolute seconds and % of relevant baseline.
- **Recommendation** — `OPEN` / `HOLD` / `REJECT`.

Honest caveats now (vs the n=1 caveat in the prior version):
- Integration cluster timings on CI depend on real Jira / ADO egress latency from GitHub-hosted runners. Multi-run data tightened the noise envelope but the *level* could shift week-to-week (Jira Cloud incidents, ADO throttling).
- 2 of the 11 BE runs had overall-failed conclusions (`26025321336`: verify-* gates; `26032405146`: 1 backend test failure). The first is included (test job green); the second is excluded from BE statistics. Their FE timings are kept where green.

---

## Per-candidate scoring (revised)

### CS-A — Cadence split: integration tests off per-PR (was: HOLD; now: HOLD as fallback)

- **Disproves**: "Real-API calls dominate PR-CI wall-clock and we can pay that cost less often without losing signal."
- **Effort**: 1 day.
- **Coverage**: Medium risk. D5 survives via release-tag cadence; drift caught later than per-PR.
- **Expected gain (revised)**: PR BE 575.8 → 575.8 − 397 = **178.8 s (-69 %)** if all integration tests move off per-PR. Same per-merge cadence as before.
- **Recommendation**: **HOLD as fallback behind CS-H.** CS-H delivers similar savings on 88 % of PRs *without* changing cadence — it filters by scope, not time. If CS-H proves too aggressive (false-negatives caught on `main`), CS-A is the safety-net.

### CS-B — Fixture session sharing (was: OPEN; now: gated by SPIKE-CS-B)

- **Disproves**: "Most of the integration runtime is auth + connection setup; per-class reuse saves the bulk of it."
- **Effort**: 2-3 days. **Estimate range unchanged at multi-run.**
- **Coverage**: Low risk.
- **Expected gain**: Same range as before (-12 % to -37 % of BE) but the multi-run Integration median (347 s on main) makes the *absolute* range 42–128 s. Calibration still depends on the un-measured setup-vs-body split.
- **Recommendation**: **GATE behind `spike-cs-b-setup-split`.** ½-day spike instruments `OneTimeSetUp` / `SetUp` / body separately; gate is setup ≥ 50 % → CS-B GO; setup < 25 % → CS-B NO-GO. Open slice-03C only post-spike.

### CS-C — Recorded cassettes (REJECT, unchanged)

- **Disproves**: "We can verify our request shape and response handling without paying real-API latency on every PR."
- **Effort**: 4-5 days.
- **Coverage**: Medium-to-high risk.
- **Expected gain (revised)**: Integration 397 → ~30-60 s replay overhead = **~340 s saved (-59 % of BE)**.
- **Recommendation**: **REJECT.** CS-H delivers comparable savings without cassette-management debt. Reopen only if CS-H + CS-G + (CS-B-or-CS-A-fallback) all underdeliver.

### CS-D — Per-spec FE fixes (refined: now gated by SPIKE-FE-profile)

- **Disproves**: "A small handful of FE specs have identifiable root causes whose fixes help both local `pnpm test` and CI in equal proportion."
- **Top-3 targets** (median across 15 FE runs):
  - `CreateConnectionWizard.test.tsx` — 17.3 s (range 12.2 – 20.4).
  - `OverviewDashboard.test.tsx` — 16.4 s (range 12.0 – 18.8).
  - `DeliveryCreateModal.test.tsx` — 8.7 s (range 6.6 – 10.0).
- **Effort**: TBD by the spike (likely 1-2 days for the top-3).
- **Coverage**: None.
- **Expected gain**: TBD by the spike — but per-test profiling is a higher-confidence path than the prior "apply standard patterns" guess.
- **Recommendation**: **GATE behind `spike-fe-profile`.** The spike produces a per-file root-cause report; the slice opens with concrete fixes named, not patterns. Same fix benefits local and CI (single-process `pnpm test` is the same code path).

### CS-E — Vitest config tuning (REJECT, downgraded)

- **Disproves**: "FE slowness is config, not test content."
- **Status**: Current config is already aggressive: `pool: "threads"`, `isolate: true`, `maxWorkers: undefined`, `fileParallelism: true`.
- **Recommendation**: **REJECT.** Headroom too small to be worth a slice. If the FE spike finds a *specific* config issue (e.g. a hidden serial barrier), it'll be folded into the FE root-cause slice.

### CS-F — Backend parallel/isolation hardening (REJECT, downgraded)

- **Status**: 2026-05-17 cache-collision learnings already shipped the bulk. Residual gain < 30 s.
- **Recommendation**: **REJECT** for this cycle. Reopen only if CS-G is somehow insufficient.

### CS-G — Cache concurrency test downscale (OPEN as slice-03A)

- **Disproves**: "Intentionally-thread-heavy unit tests scale poorly on CI's 2-vCPU runners and dominate BE wall-clock."
- **Effort**: ½ day.
- **Coverage**: None. Same invariants, smaller stress dimension. Mutation kill rate verified post-merge.
- **Expected gain (revised)**: Cache 142 s mean → ~15-25 s = **-117 ± 13 s = -22 % of BE (multi-run-confirmed)**. Original n=1 estimate was -21 %; multi-run is essentially the same.
- **Recommendation**: **OPEN as slice-03A.** Ranking unchanged; cheapest first move.

### CS-H — Path-scoped integration test selection (NEW; OPEN as slice-03B)

- **Disproves**: "Integration tests must run on every PR; we can't safely scope them by which paths changed."
- **Effort**: 2 days.
- **Mechanism**: per-connector category sub-tags (`JiraIntegration`, `AdoIntegration`, `LinearIntegration`, `GithubIntegration`); `Scripts/test-selection/select-tests.{sh,ps1}` resolver reads `git diff --name-only`; default local UX = `dotnet test` skips all integrations; explicit `--full` opt-in available; `main`-cadence forced-full guarantees post-merge coverage. Stryker per-feature configs gain a sibling `test-case-filter` so mutations against connector folders include the matching category, mutations elsewhere skip integrations (faster mutation, same kill quality).
- **Coverage**: D5 preserved on `main` (every API call still exercised per merge). Per-PR detection latency relaxed for unchanged connectors — at most one merge of delay.
- **Expected gain**: **−322 s on the 88 % of PRs that don't touch a connector = −56 % BE wall-clock weighted average.** Worst case (PR touches Jira): 0 saving (= today).
- **Local benefit**: `dotnet test` (no args) finishes ~5 minutes faster on a clean checkout with no API tokens — and no longer throws `NotSupportedException`.
- **Recommendation**: **OPEN as slice-03B** after slice-pre (labelling) and slice-03A (CS-G) land. The single biggest win in the catalog.

### CS-I — Vitest CI sharding (REJECTED before opening)

- **Disproves**: nothing yet — prior experience.
- **Reason for rejection**: User has tried Vitest sharding before; effort to wire shard merging exceeded the wall-clock saving. Local `pnpm test` is unchanged by sharding (no help). The user redirected FE effort toward profile-driven local fixes (see CS-D refined + SPIKE-FE-profile).
- **Recommendation**: **REJECT.** Documented for future reference so it doesn't reappear in a third memo iteration.

---

## Combinations

### Pipeline: slice-pre → CS-G → CS-H *(top recommendation)*

1. slice-pre (½ d): labelling fix.
2. CS-G (½ d): -22 % BE.
3. CS-H (2 d): -56 % BE on 88 % of PRs.

Cumulative on the average PR: BE 575 s → ~141 s (−75 %) and local `dotnet test` similar. No cadence change, no cassette infrastructure, no coverage regression on `main`.

### Pipeline: SPIKE-FE-profile → FE root-cause slice *(in parallel)*

½-day spike → 1-2 d slice. Independent stack from BE work; can land in any order relative to the BE pipeline. Result helps `pnpm test` locally and FE CI equally.

### Pipeline: SPIKE-CS-B → maybe CS-B *(after BE pipeline)*

Open the spike after CS-G + CS-H have landed and the new baseline is captured. If gate opens CS-B, slice-03C addresses the residual Jira integration time (which CS-H can't help with on PRs that touch Jira).

### Rejected combinations

- **CS-A + anything** — CS-H dominates CS-A's strict cadence-split with finer-grained scoping.
- **CS-F + CS-D** — CS-F's residual gain is too small; CS-D is being refined via spike.
- **CS-C alone** — rejected outright; cassette debt outweighs gains given CS-H's reach.

---

## Ranking — top picks to open

| Order | Item | Effort | Why this order |
|---|---|---|---|
| 1 | **slice-pre — integration labelling fix** | ½ d | Prerequisite for CS-H; immediate local clarity win |
| 2 | **slice-03A — CS-G cache concurrency downscale** | ½ d | Cheapest, lowest-risk, local + CI gain |
| 3 | **slice-03B — CS-H path-scoped integrations** | 2 d | Biggest single saving; preserves coverage via main cadence |
| 4 | **spike-fe-profile** | ½ d | Reveals local-friendly FE root causes |
| 5 | **slice-fe-root-cause-refactor** | TBD | Driven by spike findings |
| 6 | **spike-cs-b-setup-split** | ½ d | Decides CS-B fate; cheap risk-reduction |
| 7 | (post-spike) **slice-03C — CS-B** or **slice-03D — CS-A-Jira-only** | TBD | Whichever the spike opens |

CS-A, CS-C, CS-E, CS-F, CS-I remain rejected / held with reasons above.

---

## What this memo doesn't cover

- **Cross-runner OS variation** (Windows/macOS verify-* jobs) — Linux-runner numbers are canonical per slice scope.
- **Setup vs body split on integration tests** — un-measured at TRX granularity. Addressed by spike-cs-b-setup-split.
- **FE per-file root causes** — un-profiled. Addressed by spike-fe-profile.
- **CS-H "shared paths" whitelist evolution** — the resolver's "must run all" list will need pruning/expansion over time; out of slice-03B's initial scope but listed as a known follow-up.
- **Telemetry / phone-home** — out (D6 + [[project-self-hosted-telemetry-gap]]).

---

## Next action

Open slices in the ranking order above. Slice-pre + slice-03A can land in either order (independent). Slice-03B requires slice-pre. The two spikes can run any time after slice-pre lands; their follow-up slices land in any order relative to the BE pipeline.
