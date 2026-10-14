# Slice 05b — The header's CHANGE summary, and the {Cycle Time} 95th

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-05b (ADO Story: to be created) · **Estimate**: ~½–1d ·
**Tier**: Community · `job_id: job-flow-coach-show-whether-flow-changed`

> Added 2026-10-14 (D53, D54; maintainer, after the slice-04 live check). Built after slice 05 ({WIP}, Total {Work
> Item Age}) and before the final quality gates (mutation testing, finalize), within this Epic.

## Goal

A reader sees in the summary header, before any panel, which shown metrics improved, got worse, moved both ways or
did not move — judged by the panels' own marks — and the {Cycle Time} panel shows the 95th percentile that forecasts
at the highest certainty lean on.

## IN

- **CHANGE segment (D53)**: a third segment of the summary header, right of NOW, so the header reads THEN │ NOW │
  CHANGE like the panels.
- One verdict-free tally **per shown metric** (not per line), from the panels' own better / worse / no-change marks,
  following each metric's fixed direction of good (lower is better for {Cycle Time}, {WIP}, {Work Item Age}; higher
  for {Throughput}):
  - ✔ Improved — at least one line better, none worse;
  - ✘ Got worse — at least one line worse, none better;
  - ± Mixed — at least one line better and at least one worse;
  - – Unchanged — no line changed.
- Groups always in that order; an empty group is left out; the metrics of one group share one line ("✔ Improved:
  {Throughput}, {WIP}"), named as the panel titles read them in the instance's Terminology ("Total {Work Item Age}").
- A line that cannot be compared (a "—" on either side) is ignored, and so is a line with no change mark (the
  {Throughput} total); a metric whose lines are all dashes is not listed. Only shown metrics count. No weighting, no
  threshold: a 1-day move counts.
- Colour backs the marks (green, red, neutral, neutral); the mark and the word carry the meaning without colour.
- The segment follows the report's loading and failure looks (DT6).
- **{Cycle Time} 95th (D54)**: Then freezes the 95th with the 85th, 70th and 50th; Now computes it; its own change and
  % change; the Metrics tab's `ForecastLevel` icon for the 95th (DT10); it counts in the CHANGE tally like every line.
  A report created before this slice reads "Not captured for this report" for Then's 95th (D27) and "—" for its
  change.
- Provisional details to confirm with a sketch at the start of DISTILL (Q11): a segment with no metric to list reads
  "—"; metrics inside a group in panel order; "Not captured" ignored like a dash; the 95th row above the 85th; a long
  group wraps under its mark on a narrow page.

## OUT

Weighting lines or metrics; any threshold below which a move does not count; a majority rule; letting the 85th lead
a metric's group (a possible later refinement, only if Mixed proves too common in real use); any percentile beyond
the 95th; verdict words or generated text (D39 stands — the four labels tally marks the panels already show);
Portfolio (slice 06 inherits both); "days since Then ended" in the header (slice 07, sketch first).

## Maintainer-approved sketch (D53)

```
│ THEN                   │ NOW                     │ CHANGE                    │
│ 3 Aug – 1 Sep 2026     │ 15 Sep – 14 Oct 2026    │ ✔ Improved: Throughput    │
│   · 30 days            │   · 30 days             │ ✘ Got worse: Cycle Time   │
│ 65 Work Items closed   │ 80 Work Items closed    │                           │
```

## Learning hypothesis

**This disproves "one look at the header says which metrics moved, without hiding the tail"** if a dogfood reader
still walks every panel before saying what moved, or if most shown metrics on dogfood reports read Mixed. Then the
per-metric rule needs rethinking — "the 85th leads" is the first candidate (D53).

## Data and dogfood moment

- Dogfood: the slice-04 live-check report (Then 3 Aug – 1 Sep 2026, 30 days; Now to 14 Oct) — created before this
  slice, so it also shows Then's 95th as "Not captured for this report" — and a fresh report on the dev instance's
  busiest Team. Show both to a reader who did not create them and ask what changed before they scroll.
- Count, across dogfood reports, how many shown metrics read Mixed.
- Demo: Team Lightspeed gives Improved and Got worse groups for the `@screenshot` at finalize.

## Acceptance criteria

AC-5b.1 … AC-5b.9 in `feature-delta.md` (US-05b).

## Dependencies

Slices 03 (the summary header it extends) and 05 (all four panels whose marks it tallies).
