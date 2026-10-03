# Slice 10 — Edit report: name, Now length, shown metrics

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-10 (ADO #6168) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

> Re-cut 2026-10-03: was "Change which claims a report shows (cancellable)", ½ day. The maintainer added renaming
> (D44) and an editor-changed Now length (D43); the shown-metrics change is folded into the same dialog. It is part
> of v1 now, no longer cancellable, and the release follows it (D34). The file name keeps its old slug so existing
> links still resolve.

## Goal

An editor changes a report's name, its Now length and which metrics it shows, in one "Edit report" dialog. Every
reader sees the change. Then — its window and its frozen data — never changes.

## IN

- "Edit report" on the report, Write only, server-guarded; no action for viewers.
- Name (not empty); Now length with the rules of D43; shown metrics with at least one ticked. Ticking a hidden metric
  reveals its panel from values frozen at creation, nothing recomputed.
- Metrics registered after creation read "Not captured for this report" and cannot be ticked (D27).
- The Then window is shown read-only.

## OUT

Editing Then or recomputing it (never, D44); a per-panel author note (later, D39); a different Now per reader (D43).

## Learning hypothesis

**This disproves "freezing every metric pays off"** (DV-6) if nobody ticks a hidden metric within a month of
release. Freezing everything stays (it is cheap and already built), but later templates need not extend it.

## Data and dogfood moment

- Dogfood: rename a dev-instance report, shorten its Now, and tick {Throughput} after a reader asks about it; check a
  Viewer sees all three changes.

## Acceptance criteria

AC-10.1 … AC-10.5 in `feature-delta.md` (US-10).

## Dependencies

Slices 05 (all panels and the picker) and 07 (the Now length rules). The release follows this slice (D34).
