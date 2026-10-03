# ADR-221: Then is frozen as captured values and unrounded XmR limits computed from its own series

- **Status**: Proposed (DESIGN, 2026-10-03)
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
2. **Extract `XmRCalculator.Limits(values)`** returning average, upper and lower limit as doubles, whether the lower
   limit was clamped, and the point count. `Calculate` calls it and rounds at its own boundary, so every existing
   chart is byte-identical (the existing `XmRCalculatorTest` suite plus one property test pin this).
3. **Count beyond limits with one pure rule** that mirrors `XmRCalculator`'s large-change rule: above = value >
   upper; below = value < lower, counted **only when lower > 0**; otherwise the below count is "—" with "Then's lower
   limit is 0" (Q8).
4. **What the payload freezes per metric**: each shown value as `{ value (double or null), sampleSize, absentReason }`
   and, for the beyond-limits count, `{ average, upper, lower, lowerClamped, pointCount }` unrounded — or an absent
   reason. Captures are taken for **every applicable metric**, ticked or not.
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
7. **Value definitions** (window `[start, end]`, L inclusive days; the same code for Then at creation and Now on read):
   - **{Cycle Time}**: the 50th / 70th / 85th percentile from the Metrics tab's percentile read (Then freezes the 85th);
     sample size = finished {Work Items} with a cycle time in the window; limits over those cycle times by close date.
   - **{Throughput}** (D47): `total` = sum of the daily finished counts; `perDay` = total ÷ L, unrounded; sample
     size = L days. Change and % change are on `perDay`, so a Now of another length compares correctly. Limits and the
     beyond-limits count over the daily counts.
   - **{WIP}**: average of the daily {WIP} (unrounded), its minimum and maximum; limits over the daily values.
   - **{Work Item Age}** (Q10, D48), from the daily total age `T[d]` and the daily {WIP} `W[d]` of the same
     population: total window average = mean of `T[d]` over all L days; total last day = `T[end]`; average window
     average = mean of `T[d] ÷ W[d]` over the days with `W[d] > 0`; average last day = `T[end] ÷ W[end]` (sample size
     `W[end]` {Work Items}). Limits and count over `T[d]`.
8. **Nothing about the owner's settings is stored** (D46). Now uses today's settings; Then stays as frozen; the
   report does not compare them.

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

- **Positive**: the frozen yardstick is independent of every PBC setting; the Metrics page is untouched; one rule
  counts beyond limits for all four metrics.
- **Positive**: thin data is shown honestly rather than hidden: "34 days · 3 Features" tells the reader exactly how
  much the number rests on (D45).
- **Negative**: a per-item lower limit is usually 0 for {Cycle Time}, so "below" will often read "—" (Q8, accepted;
  slice 03 tests whether the count says anything on real Teams).

## Enforcement

- ArchUnitNET: the Reports module does not depend on `ProcessBehaviourChart`, `XmRResult` or
  `WorkTrackingSystemOptionsOwner.ProcessBehaviourChartBaseline*`.
- NUnit: `Calculate` equals `Limits` rounded, for generated series; the beyond-limits rule equals the large-change
  rule's classification on the same data.
- NUnit (AC-2.3): Then captured for a window equals the Metrics endpoints' values for the same window and settings.
- NUnit: changing the owner's PBC Baseline after creation changes no frozen value and no count (AC-3.2).

Cross-refs: [ADR-208](./adr-208-a-past-day-is-computed-by-the-recorders-own-code.md) (one computation, two callers), ADR-219,
ADR-220.
