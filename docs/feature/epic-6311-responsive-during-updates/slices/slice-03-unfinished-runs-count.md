# Slice 03 — Runs that do not finish count, and every forecast says so

**Story** #6309 / US-03 · **Job** `job-forecast-no-false-certainty`

## Goal

A forecast percentile that the simulated runs did not reach within the horizon reads as "beyond the
horizon" everywhere a forecast date appears. It is never a date taken from the luckier runs, and never
today. The horizon itself is still 100,000 days.

## IN scope

- Unfinished runs counted as beyond the horizon in the distribution; percentiles and likelihoods computed
  over all runs (D6, S9). Completion and start forecasts; multi-team aggregation.
- An **additive** beyond-horizon marker on the wire (NMD-1, D12).
- Every web surface in S12 rendering it with one shared wording (copy from the DISTILL walk-through).
- Pinned closure records and metric snapshots round-trip the marker.
- Write-back writes nothing for a beyond-horizon percentile (NMD-2).
- Correct the `HowTheRunEnded.RanOutOfDays` comment, which claims no data can reach the horizon (S8).

## OUT of scope

- Lowering the horizon (slice 04). The Lighthouse-Clients rendering, which is a clients-repo release at
  finalize (NMD-1 option c). The PDF export (Epic #6298 coordinates).

## Learning hypothesis

**Disproves that every forecast surface can carry a non-date without breaking.** The timeline bar, the
predictability chart, pinned closure records and the export table all assume a date. If any of them cannot
carry the marker honestly, slice 04 must not ship until that surface has a design. Releasable alone: it
removes today's biased dates and the "today" date for Features beyond 274 years.

## Acceptance criteria

AC-03.1 to AC-03.8. Key: **AC-03.1** (the per-percentile rule), **AC-03.2** (never today), **AC-03.4**
(additive wire), **AC-03.5** (every surface, one wording), **AC-03.8 (production data)** the before/after list
of Features whose forecast changes on the restored dev instance, read by the maintainer.

## Dependencies

NMD-1 and NMD-2 answered. The DISTILL UI walk-through (UI-1, UI-2) done.

## Effort / reference class

About 1 day; the largest slice. Reference class: forecast-confidence-cap (#5126), one label rule across
every likelihood surface.

## Pre-slice SPIKE

None. DESIGN Q4 (sentinel versus counted field) is a design choice, not an unknown.
