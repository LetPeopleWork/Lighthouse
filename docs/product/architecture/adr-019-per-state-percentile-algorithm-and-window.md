# ADR-019: Per-State Age-at-State-Exit Percentile Algorithm and Window Semantics

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-020/021 as the three DESIGN decisions for `aging-pace-percentiles`)
**Date**: 2026-05-24
**Feature**: aging-pace-percentiles (Epic 4144 MVP bundle, slice F)
**Decider**: Morgan (Solution Architect)

---

## Amendment (2026-05-25) — feature simplification

The feature was scoped down with the user to "colored bands + one on/off toggle, nothing more". This ADR is amended; the body below is **superseded where it conflicts** with the points here (see `aging-pace-percentiles/feature-delta.md` D12, DDD-1, DDD-4):

1. **Duration formula is now cumulative total age at state exit, not per-state duration.** The observation for a `(item, state, visit)` is `exitTransition.TransitionedAt − item.StartedDate` (the item's *total* work-item age at the moment it left the state) — **not** `exit − entry`. Reason: the chart's Y axis is total work-item age, so a band must be in total-age units to be comparable to the dots above its column; this also makes the bands rise monotonically left→right through the workflow (ActionableAgile parity). The `exit − entry` formula in §1 / §3 / the §A "max/sum" discussion is superseded — but **visit-level sampling is retained** (re-entries still contribute separate observations, each measured as total-age-at-that-exit).
2. **No `sampleSize` and no low-sample handling.** §4's DTO drops `SampleSize`; §5's "n<10 low-sample tooltip" rules are **removed entirely** (the FE shows no low-sample messaging). States with zero observations are still omitted; everything else returns its bands. The DTO is `AgeInStatePercentilesDto(string State, IReadOnlyList<PercentileValue> Percentiles)`.
3. **US-03 removed.** §B's "accepted for the FE-side US-03 tooltip computation" no longer applies — there is no dot tooltip annotation and this feature does not read `CurrentStateEnteredAt` at all.

Unchanged: the "completed in window" membership rule (§2, `ClosedDate ∈ window`), the percentile function (§4, `PercentileCalculator`, defaults 50/70/85/95), and the caching policy (§6).

---

## Context

DISCUSS locked the user-facing contract: per-state percentile bands at 50/70/85/95 (D2) of the historical distribution of age-at-state-exit (D1), computed over the team's existing history window (D3), for completed items only (implicit in D5: empty/low-sample handling is keyed on the completed-item count per state). DESIGN must pin down precisely:

1. **What counts as a "sample"** — what `(workItem, state)` pair contributes one observation of `ageAtStateExit`?
2. **What is the window-membership rule** — when is an item considered "completed in window"?
3. **What is the duration formula** — given two transitions, what is `ageAtStateExit`?
4. **What is the percentile function** — given a list of durations, what algorithm produces the 50/70/85/95 values?
5. **What is the empty / single / low-sample behaviour** at the API boundary?

These are the questions a software-crafter would otherwise have to invent during GREEN. Inventing them produces small but real divergence between the team-scope and portfolio-scope implementations and between this feature and the sibling MVP features that read the same `WorkItemStateTransition` rows. Pinning them in an ADR keeps both scopes and both sibling features computing comparable values.

Available primitives from sibling ADR-015 / ADR-016:

- `WorkItemStateTransition { Id, WorkItemId, FromState, ToState, TransitionedAt }` with index `(WorkItemId, TransitionedAt)`.
- `WorkItem.CurrentStateEnteredAt: DateTime?` (sync-time persisted; updated only by `WorkItemService.RefreshWorkItems`).
- `WorkItem.StartedDate: DateTime?` and `WorkItem.ClosedDate: DateTime?` (unchanged, existing semantics).
- Existing `PercentileCalculator.CalculatePercentile(List<int>, int)` — sorts ascending, index `= floor(percentile / 100 * count) - 1`, clamped to `[0, count - 1]`, returns `int` (days).

Sibling F DISCUSS lock D5: "empty distribution → omit; low-sample (n<10) → still compute, presentational tooltip surfaces 'low sample'; bimodal → out of scope (percentile math still defined)". DESIGN must make the math concrete without re-litigating those user-facing rules.

Sibling F DISCUSS lock D3: "window matches the existing `cycleTimePercentiles` endpoint's `startDate` / `endDate`" — i.e. the team's configured History window.

---

## Decision

### 1. Sample definition

For a given workflow state `S` and a given work item `W` that was **completed within `[startDate, endDate]`** (definition below), every **completed visit** of `W` through `S` contributes one observation:

```
# AMENDED 2026-05-25 (D12): cumulative total age at exit, NOT exit − entry
observation(W, S, visit_i) = exitTransition_i.TransitionedAt - W.StartedDate
```

where, ordered by `TransitionedAt` ascending across `W.Transitions`:

- `entryTransition_i` is the i-th transition whose `ToState == S`.
- `exitTransition_i` is the next transition (by `TransitionedAt`) whose `FromState == S`.

A visit is **completed** when `exitTransition_i` exists in the window's transition data. If the item is still currently in `S` at query time (i.e. it has an `entryTransition_i` but no subsequent transition leaving `S`), that visit is **not** counted by this endpoint — those in-flight items are the consumers of the bands (the chart's dots), not contributors to the distribution.

Items that re-entered `S` multiple times during their lifetime contribute multiple observations (one per completed visit). This is the only correct interpretation of "historical age-at-state-exit per state" — re-work that loops through Review is part of the historical pace pattern for Review and must influence the percentile.

Bucket the observations by `S` (i.e. by `entryTransition_i.ToState` == `exitTransition_i.FromState`). Each bucket's count is `sampleSize`.

Observation values are computed as an integer day count using the existing `WorkItemBase.GetDateDifference` convention (date-only diff, ceiling) — the same convention that produces `CycleTime` and `WorkItemAge`. Software-crafter selects the exact call shape at GREEN; the architectural requirement is that day-counting is consistent with the rest of the codebase. Zero-duration observations (entry and exit on the same UTC day) round to `0` days and are kept (they are meaningful: "items typically pass through this state in less than a day" is a valid percentile story).

### 2. "Completed in window" item-membership rule

A work item `W` is **completed in `[startDate, endDate]`** iff `W.ClosedDate` is non-null AND `W.ClosedDate` is within the window (inclusive both ends, day-resolution comparison matching the existing `GetWorkItemsClosedInDateRange` predicate on `TeamMetricsService`).

This rule **deliberately mirrors** the existing `GetCycleTimePercentilesForTeam` membership rule (`TeamMetricsService.cs:303-320`). The aging-pace-percentile distribution is a per-state companion to the full-cycle-time percentile distribution; using the same membership rule means a user reading both percentile families is reading distributions over the **same set of completed items**. Diverging membership rules would silently change the comparison.

This rule is **explicitly different** from sibling `state-time-cumulative-view`'s D12 inclusion rule (frame-based: any item whose timeline intersected the window contributes). The two features answer different questions and the difference must be visible at the membership level. ADR-021 (below) decides the consequence for service-layer code sharing.

### 3. Duration formula

Per sample definition (1) above:

```
# AMENDED 2026-05-25 (D12): cumulative total age at exit, NOT exit − entry
ageAtStateExit = exitTransition.TransitionedAt - W.StartedDate
```

Day-counting convention: project's existing `GetDateDifference` (date-only diff, ceiling — same as `WorkItemAge`).

Per visit, not per item — re-entries are independent observations.

### 4. Percentile function

Reuse the existing `PercentileCalculator.CalculatePercentile(List<int>, int)`. Same algorithm as `cycleTimePercentiles` produces — bands at 50, 70, 85, 95 (D2) are the same arithmetic, just computed over a different sample set.

Concrete shape per state:

```
// AMENDED 2026-05-25 (DDD-4): SampleSize dropped — no low-sample messaging
public sealed record AgeInStatePercentilesDto(
    string State,
    IReadOnlyList<PercentileValue> Percentiles);
```

The endpoint returns `IReadOnlyList<AgeInStatePercentilesDto>` — one entry per state that has at least one observation, ordered by the team's workflow `doingStates` order (same order the chart's X-axis uses, sibling F D6).

### 5. Empty / low-sample behaviour at the API

- **Zero observations for state `S`** (no completed visit through `S` for any completed-in-window item): omit the `AgeInStatePercentilesDto` entry for `S` entirely. The chart's "no per-state bands for that state" rendering (US-01 AC line 4) is driven by the omission.
- **Zero observations across all states** (a brand-new team with no completed items in window): return an empty list. The chart's existing full-width cycle-time bands continue to render (US-01 AC line 4).
- **One observation only**: return the dto with `SampleSize = 1` and four identical percentile values (all percentiles collapse to that single sample). The legend tooltip ("low sample") surfaces in US-02 from `SampleSize < 10`.
- **`SampleSize < 10`**: still return the dto with computed values. The FE legend tooltip (US-02 slice 02) renders the low-sample warning.

No backend-side threshold suppresses returned bands. Suppression / annotation is purely presentational and lives on the FE.

### 6. Caching policy

Reuse the existing `BaseMetricsService.GetFromCacheIfExists` pattern with cache key `AgeInStatePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}` (scoped per Team / per Portfolio, matching the existing per-entity cache key namespace established by `cycleTimePercentiles`). The cache is invalidated by the existing post-sync invalidation hook (the same hook that invalidates `CycleTimePercentiles_...` keys today). No new cache-invalidation infrastructure.

---

## Alternatives Considered

**Option A — "Item-level" percentile instead of "visit-level": one observation per (item, state) pair = the maximum (or sum) of all that item's visit durations through that state.**

- Pros: simpler mental model ("each item contributes once per state").
- Cons: silently penalises re-work. An item that bounced into and out of Review three times (2d + 1d + 5d = 8d total / 5d max) would contribute one inflated number to the Review distribution, hiding that most individual visits to Review take 1–2 days. The flow coach's question is "is this in-flight visit pacing slow vs the historical visit?" — visit-level matches that question; item-level conflates the question with a rework-count question.
- **Rejected** because the user-facing question (US-01 elevator pitch: "the work this item is doing IN THIS STATE is slower than 85% of similar work") is fundamentally about the single visit the dot represents.

**Option B — Use the `currentStateEnteredAt` field as the entry timestamp instead of walking transitions.**

- Pros: zero transition-table lookup for the in-flight dot's "what percentile am I at" computation. (Relevant only for US-03 tooltip, not for the historical distribution.)
- Cons: `currentStateEnteredAt` is the entry timestamp for the item's CURRENT state ONLY. It cannot reconstruct the historical visits to other states or the historical re-visits to the current state. The historical distribution **must** walk transitions.
- **Rejected** for the distribution side; **accepted** for the FE-side tooltip computation in US-03 — the dot's `daysInState` value already comes from `currentStateEnteredAt` via the sibling's `WorkItemDto.CurrentStateEnteredAt`. ADR-021 records this client-side computation rule.

**Option C — Window-clipping the visit duration: an observation contributes only the portion of its `(entry, exit)` interval that falls within the window.**

- Pros: bands describe "how much time items spent in this state DURING the window".
- Cons: that is **not** the user's question. The user is asking about historical pace: "did items that finished in this window typically spend N days in Review?" The answer is the full visit duration, regardless of whether part of it predates the window. Clipping would systematically under-state the band heights for items that started before the window and finished inside it (the common case for long-lived states like Review on items that take >2 weeks end-to-end). This is also explicitly the OPPOSITE of sibling state-time-cumulative-view's D5 "full-duration attribution" — but only by accident; the two features happen to agree on the *duration* side, while disagreeing on the *membership* side. (Sibling B3 includes items by frame intersection; sibling F includes by ClosedDate-in-window.)
- **Rejected** because clipping destroys the comparability with the existing `cycleTimePercentiles` distribution (which is also unclipped).

**Option D — Use a different percentile interpolation method (linear interpolation between samples, R-style "Type 7", numpy default).**

- Pros: more standard statistically; smoother values for small samples.
- Cons: would diverge from the existing `cycleTimePercentiles` algorithm. The user reads both percentile families on the same chart; using the same algorithm keeps the visual comparison fair. Numerical accuracy at MVP scale (typically 20-200 observations per state per quarter) is bounded by the small-sample noise, not by the interpolation method; the existing nearest-rank-with-clamp algorithm is good enough.
- **Rejected** to preserve algorithmic parity with `cycleTimePercentiles`. (If a future ADR decides to upgrade the percentile algorithm, it must upgrade both endpoints together — this is a sensitivity point, not a trade-off, per ATAM.)

---

## Consequences

**Positive**:

- Sample, membership, duration, and percentile rules are explicit and aligned with the existing `cycleTimePercentiles` rules. A user reading both percentile families on the same chart is reading distributions computed with consistent semantics.
- Visit-level observation matches the user's chart-glance question precisely. Re-work surfaces as elevated bands for the state that experiences the re-work, which is correct.
- Empty / low-sample / single-sample behaviour is uniform: the API returns what it has; presentation rules live on the FE.
- The endpoint reuses `PercentileCalculator`, `GetFromCacheIfExists`, the existing window-parameter validation pattern, and the existing `GetWorkItemsClosedInDateRange` predicate. Net new backend computation is the transition-grouping walk; everything else is composition of existing primitives.

**Negative**:

- The visit-pair walk (`for each completed item: walk transitions ordered, pair entry→next-exit transitions, bucket by state`) is per-item work proportional to transitions-per-item. For an item with 12 transitions (typical for a long-lived feature passing through To Do / In Progress / Review / Test / Done with a few rework loops), this is 12 comparisons. For 200 completed items in a quarter, 2400 comparisons per request — trivial. Profiling at MVP scale is not expected to require caching beyond the existing `GetFromCacheIfExists` hook. (The pre-slice spike candidate in `slice-01-per-state-bands-team.md` flags a 6-month-of-transitions profiling check; this ADR's algorithm meets that bar.)
- The membership-rule divergence vs sibling B3 is documented but not enforced by the type system. The risk is that a future shared helper conflates the two — ADR-021 below explicitly forbids that helper.

**Neutral**:

- The day-counting convention (`GetDateDifference` style) means observations are integer days. Sub-day visits round up to `1`. This matches the existing `CycleTime` convention and is the right unit for the chart (the chart's Y-axis is days).
- Cache key namespace shares the `Team` / `Portfolio` per-entity cache scope; cache invalidation is inherited from the existing post-sync hook. No new invalidation rule.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| The percentile values for `aging-pace-percentiles` are computed via `PercentileCalculator.CalculatePercentile` (the SAME function used by `cycleTimePercentiles`) | NUnit test in `TeamMetricsServiceTests.cs` asserts that `GetAgeInStatePercentilesForTeam` calls into `PercentileCalculator` and produces values consistent with a hand-computed fixture using the same algorithm |
| "Completed in window" predicate matches `GetWorkItemsClosedInDateRange` (the predicate used by `cycleTimePercentiles`) | NUnit test asserts that for a fixture with an item closed exactly at `startDate` and another at `endDate + 1`, the first contributes and the second does not — identical to the cycleTimePercentiles boundary test |
| Visit-level (not item-level) sampling: an item with N completed visits through state `S` contributes N observations | NUnit test in `TeamMetricsServiceTests.cs` against a fixture with one item that has 3 completed visits through `Review` of durations 2 / 5 / 1 days — asserts the Review bucket contains exactly `[1, 2, 5]` |
| Cache key uses the date-stamped pattern shared with `cycleTimePercentiles` | NUnit test inspects the key passed to `GetFromCacheIfExists` matches the `AgeInStatePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}` shape |

---

## Cross-feature impact

- `time-in-state-and-staleness` (sibling 1, foundation): unchanged. This ADR consumes the primitives shipped by ADR-015 / ADR-016 / ADR-017 exactly as specified.
- `state-time-cumulative-view` (sibling B3): membership rule is explicitly different (frame-based intersection vs ClosedDate-in-window) and duration rule is explicitly different (full-duration attribution with NO membership filter vs unclipped duration with ClosedDate-in-window filter). The two endpoints are intentionally not unifiable behind a shared helper — see ADR-021.
- Future configurable percentiles (deferred ADO #5076): the algorithm is parameterised on a `IReadOnlyList<int>` of requested percentiles; defaults are wired at the controller, not in the algorithm. Adding configurability later is a plumbing change, not an algorithm change.
