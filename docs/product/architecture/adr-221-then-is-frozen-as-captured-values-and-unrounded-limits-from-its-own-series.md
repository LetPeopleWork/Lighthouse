# ADR-221: Then is frozen as captured values computed from its own series

*Title amended 2026-10-14. Was: "Then is frozen as captured values and unrounded XmR limits computed from its own
series" — the limits went with D49; see "Amended 2026-10-14" below. The file name keeps the old words so links resolve.*

- **Status**: Accepted (2026-10-13, after the Final Wave Review; proposed in DESIGN 2026-10-03). **Amended 2026-10-14**
  (maintainer decisions D49–D51) — still Accepted.
- **Date**: 2026-10-03
- **Feature**: epic-5878-baseline (slices 01, 02, 03, 04, 05)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

D26 and D40: at creation, Then's shown values and Then's XmR average and limits are frozen, **unrounded**, for every
metric that applies to the owner (not only the ticked ones); Now's points are counted against those limits, split
above / below; the limits never come from the owner's PBC Baseline setting nor from Now. Whether the full Then series
is stored is DESIGN's call. D41 (wording since superseded by D45): no new thresholds, never a misleading 0.

What the code does today (verified):

- `XmRCalculator.Calculate(baselineValues, displayValues)` computes average and limits as doubles, clamps a
  negative lower limit to 0, then returns them **rounded to integers** in `XmRResult`. Fewer than 2 baseline values →
  limits equal the average and no point is classified.
- The PBC builders in `BaseMetricsService` take the owner's `ProcessBehaviourChartBaseline*` when set, and return
  **no data points at all** when that baseline fails validation — so reading Then through them couples the report to
  a setting it must ignore.
- **There is no percentile minimum-data guard.** `PercentileCalculator` returns 0 for an empty list; the Metrics
  widget shows "No data available" only for an empty array, which the backend never sends. The only existing
  boundaries are: no finished item to measure, a collapsed band (`Average == 0 && Upper == 0`, the PBC snapshot
  writer's honesty gate) and fewer than 2 points for a moving range.

The maintainer then decided (2026-10-03, after this design's first draft): every value is shown with its sample size
and "—" appears only when truly empty (D45); no settings snapshot or notice (D46); {Throughput} as total and per-day
average (D47); average {Work Item Age} as window average and last day (D48).

## Decision

1. **Read Then's series through `IReportMetricSeries`** (ADR-220): the same raw series the Metrics page builds its
   charts from — finished items' cycle times (cycle time > 0, ordered by close date then id), daily {Throughput},
   daily {WIP}, daily total {Work Item Age} — never through a `Get*ProcessBehaviourChart` method.
2. ~~**Extract `XmRCalculator.Limits(values)`** returning average, upper and lower limit as doubles, whether the lower
   limit was clamped, and the point count. `Calculate` calls it and rounds at its own boundary, so every existing
   chart is byte-identical (the existing `XmRCalculatorTest` suite plus one property test pin this).~~ *(superseded by
   D49, 2026-10-14)*
3. ~~**Count beyond limits with one pure rule** that mirrors `XmRCalculator`'s large-change rule: above = value >
   upper; below = value < lower, counted **only when lower > 0**; otherwise the below count is "—" with "Then's lower
   limit is 0" (Q8).~~ *(superseded by D49, 2026-10-14)*
4. **What the payload freezes per metric**: each shown value as `{ value (double or null), sampleSize, absentReason }`
   ~~and, for the beyond-limits count, `{ average, upper, lower, lowerClamped, pointCount }` unrounded — or an absent
   reason~~ *(limits superseded by D49, 2026-10-14)*. Captures are taken for **every applicable metric**, ticked or not.
5. **The full Then series is not stored.** No panel or chart needs it (D39), D27 already says later values read "Not
   captured for this report", and the payload can gain a series member additively if a later slice ever needs one.
6. **Every value carries its sample size and the panel always shows it** (D45). A value is absent only when there is
   nothing to compute, as closed reason codes — no threshold:
   - `no-finished-items` — no finished {Work Item} with a cycle time in the window (percentiles; per-item limits);
   - `too-few-points` — fewer than 2 points for a moving range (limits);
   - `no-process` — a collapsed band, average and upper limit both 0 (e.g. no {Throughput} at all in Then);
   - `no-wip` — no {Work Item} in progress, so no average {Work Item Age} (D48);
   - `lower-limit-zero` — below count only (Q8);
   - `then-was-zero` — % change only, computed by the panel (Q9).
   *(Amended 2026-10-14: only `no-finished-items` remains on the wire, and `then-was-zero` in the panel; the other
   four went with D49 and D51.)*
7. **Value definitions** (window `[start, end]`, L inclusive days; the same code for Then at creation and Now on read):
   - **{Cycle Time}**: the 50th / 70th / 85th percentile from the Metrics tab's percentile read (Then freezes the 85th);
     sample size = finished {Work Items} with a cycle time in the window; limits over those cycle times by close date.
   - **{Throughput}** (D47): `total` = sum of the daily finished counts; `perDay` = total ÷ L, unrounded; sample
     size = L days. Change and % change are on `perDay`, so a Now of another length compares correctly. Limits and the
     beyond-limits count over the daily counts.
   - **{WIP}**: average of the daily {WIP} (unrounded), its minimum and maximum; limits over the daily values.
     *(Amended 2026-10-14 by D50: Average, Median, Min, Max of the daily {WIP}; no limits.)*
   - **{Work Item Age}** (Q10, D48), from the daily total age `T[d]` and the daily {WIP} `W[d]` of the same
     population: total window average = mean of `T[d]` over all L days; total last day = `T[end]`; average window
     average = mean of `T[d] ÷ W[d]` over the days with `W[d] > 0`; average last day = `T[end] ÷ W[end]` (sample size
     `W[end]` {Work Items}). Limits and count over `T[d]`. *(Amended 2026-10-14 by D51: Average, Median, Min, Max of
     `T[d]` only; no per-item average, no last day, no limits.)*
   - *(Amended 2026-10-14: the {Cycle Time} limits and the {Throughput} limits and count above are gone too, D49.)*
8. **Nothing about the owner's settings is stored** (D46). Now uses today's settings; Then stays as frozen; the
   report does not compare them.
9. **Then freezes the {Cycle Time} 85th percentile only** (2026-10-13). The panel's Then column shows the 85th alone,
   as in the approved mockup; Now shows the 85th, 70th and 50th. Then's 70th and 50th were never Then's, so no "Not
   captured" sentence appears for them; that sentence stays for a metric or value the report predates (D27).
10. **On the wire the two sides of a metric are `atThen` and `atNow`** (2026-10-13), in the read and in the browser
   alike. A member named `then` is a Biome error (`noThenProperty`) and turns the object into a thenable for anything
   that awaits it. Member names inside a side: `p85`, `p70`, `p50`; `total`, `perDay`; `average`, `min`, `max`;
   `totalWindowAverage`, `totalLastDay`, `averageWindowAverage`, `averageLastDay`; each `{ value, sampleSize,
   absentReason }`. `beyondThenLimits` is `{ above, below }` of the same shape, the count in `value`. *(Amended
   2026-10-14: `wip` and `work-item-age` are both `average`, `median`, `min`, `max`; `beyondThenLimits` is dropped.)*

## Alternatives considered

- **Call the existing PBC methods with display window = Then.** Limits come from the owner's PBC Baseline whenever
  one is set, are rounded, and vanish when that setting is invalid. **Rejected** (S4, S3).
- **Overload the PBC builders with an explicit baseline window.** Touches the Metrics page's code path to serve the
  report and still returns rounded integers. **Rejected** (guardrail: Metrics tab unchanged).
- **Store the full Then series as well** (a few KB per report). Keeps future values derivable, but has no consumer
  and invites "recompute Then from the series" — the opposite of frozen. **Rejected for v1**, additive later.
- **A minimum-data threshold** (e.g. 8 finished items, as an earlier DISCUSS draft had), hiding thin values behind
  "—". **Rejected by the maintainer** (D45): show the value and its sample size instead.
- **A {Throughput} weekly median with a week count** (the original D39 shape). **Rejected by the maintainer** (D47):
  partial weeks distort it, and a Now of a different length would need re-bucketing; the per-day average over the
  whole window compares any two lengths directly.

## Consequences

- **Positive**: the frozen yardstick is independent of every PBC setting; the Metrics page is untouched; ~~one rule
  counts beyond limits for all four metrics~~ *(D49: there is no count)*.
- **Positive**: thin data is shown honestly rather than hidden: "34 days · 3 Features" tells the reader exactly how
  much the number rests on (D45).
- ~~**Negative**: a per-item lower limit is usually 0 for {Cycle Time}, so "below" will often read "—" (Q8, accepted;
  slice 03 tests whether the count says anything on real Teams).~~ *(moot since D49)*

## Enforcement

- ArchUnitNET: the Reports module does not depend on `ProcessBehaviourChart`, `XmRResult` or
  `WorkTrackingSystemOptionsOwner.ProcessBehaviourChartBaseline*`.
- ~~NUnit: `Calculate` equals `Limits` rounded, for generated series; the beyond-limits rule equals the large-change
  rule's classification on the same data.~~ *(withdrawn 2026-10-14, D49; instead ArchUnitNET: the Reports module
  does not depend on `XmRCalculator` either)*
- NUnit (AC-2.3): Then captured for a window equals the Metrics endpoints' values for the same window and settings.
- NUnit: changing the owner's PBC Baseline after creation changes no frozen value ~~and no count~~ (AC-3.2). *(count
  gone since D49, 2026-10-14)*
- NUnit: the read DTO has no member named `then` or `now`; Biome's `noThenProperty` holds the frontend.

## Alternatives considered for the wire names (2026-10-13)

- **`then` / `now` on the wire, renamed to `atThen` / `atNow` in the client** (what DISTILL first pinned). Works,
  but keeps two names for one thing and a mapping step every consumer (5882's renderer, a later CLI) must repeat.
  **Rejected.**
- **`thenValues` / `nowValues`.** Equally safe; `atThen` / `atNow` was preferred because the frontend model and its
  tests already use it. **Rejected.**

## Amended 2026-10-14

**Context.** At the DISTILL sketch review for slices 03–05 the maintainer decided D49–D52: "start simple" — no panel
counts Now's points beyond Then's XmR limits, and Then no longer freezes an XmR average or limits (D49); {WIP} reads
Average, Median, Min and Max of the window's daily values, each with its own change (D50); the {Work Item Age} panel
becomes "Total {Work Item Age}", the same four lines over the daily total age, with no per-item average and no
last-day value (D51); a summary header carries each side's window and closed count (D52, frontend only). Slices 01–02
are shipped; their stored reports hold `cycle-time` values only.

**Decision.**

1. **Then freezes captured values only.** No limits member in any capture; no beyond-limits count on the wire
   (`beyondThenLimits` dropped — no released backend ever sent it). `XmRCalculator` is not extended and the Reports
   module must not depend on it (ArchUnitNET, added to the existing rule that keeps it off `ProcessBehaviourChart`
   and `XmRResult`). The extraction made in c206d65bd was reverted in 058b4ba48.
2. **Value shapes**: `cycle-time` `{ p85, p70, p50 }` (unchanged); `throughput` `{ total, perDay }` (unchanged but
   for the limits); `wip` and `work-item-age` both `{ average, median, min, max }` over the window's L daily values
   (daily {WIP}; daily total age). Each value `{ value, sampleSize, absentReason }`; for a daily-series value
   `sampleSize` = L days.
3. **Median** = `PercentileCalculator.CalculatePercentile(values, 50)`, the Metrics tab's 50th: the value at 1-based
   position max(1, ⌊n ÷ 2⌋) of the sorted values — the lower of the two middle values for an even count, the value
   just below the middle for an odd one. Chosen so a report's Median and its 50th are never computed two ways. The
   function sorts its input in place, so it is given a copy.
4. **Absence**: one code on the wire, `no-finished-items` ({Cycle Time}); {Throughput}, {WIP} and Total {Work Item
   Age} are never absent — a window has ≥ 14 days and a 0 is a real 0. `then-was-zero` stays the panel's (Q9).
5. **Display**: the panel rounds with `roundForDisplay` — 0 decimals for {WIP} and Total {Work Item Age} (as {Cycle
   Time}), 2 for {Throughput} per day — and computes change and % change from the displayed values.

**Alternatives considered.**

- **Keep the frozen limits but hide the count** (store now, show later). Costs a payload member and an extraction
  nobody reads, and invites a second meaning of "Then" before anyone asked for it. **Rejected**: the payload is
  additive, so limits can be frozen by a later version for reports created from then on.
- **Textbook median** (mean of the two middle values for an even count). Gives a half-day the reader never saw on
  any day and disagrees with the report's own {Cycle Time} 50th for the same data. **Rejected**; changing the
  convention is a Metrics-wide decision.
- **Separate shapes for {WIP} and Total {Work Item Age}** (keeping `totalWindowAverage` etc.). Two panel layouts for
  one idea. **Rejected** (D50 and D51 define the same four lines).

**Consequences.**

- **Positive**: less to build and test (no extraction, no rule, four absence codes gone); the Metrics page is not
  touched at all; two panels share one summary and one layout.
- **Negative**: a report says nothing about whether a change is beyond routine variation; Signals (Epic 5935) owns
  that question, and a process-behaviour entry may join the report later (D49).
- **Neutral**: reports already stored are unaffected (they hold `cycle-time` values only).

Cross-refs: [ADR-208](./adr-208-a-past-day-is-computed-by-the-recorders-own-code.md) (one computation, two callers), ADR-219,
ADR-220.
