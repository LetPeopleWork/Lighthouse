# Slice 03 — Runs that do not finish count, and every forecast says so

**Story** #6309 / US-03 · **Job** `job-forecast-no-false-certainty` · **Released** together with slice 04
(US-05) under Story #6309 (NMD-5)

## Goal

A forecast percentile that the simulated runs did not reach within the horizon reads as "beyond the
horizon" everywhere a forecast date appears. It is never a date taken from the luckier runs, and never
today. The horizon itself is still 100,000 days.

## IN scope

- Unfinished runs counted as beyond the horizon in the distribution, and runs ended by a dependency circle
  too (NMD-13); percentiles and likelihoods computed over all runs (D6, S9). Completion and start forecasts;
  multi-team aggregation.
- An **additive** beyond-horizon marker on the wire, `beyondHorizon` + `horizonYears`, on the forecasts and on
  the Delivery history points (NMD-1, NMD-3, NMD-12, D12).
- Every web surface in S12 rendering it with one shared wording, "More than 5 years" (NMD-9, NMD-10).
- Pinned closure records and metric snapshots round-trip the marker.
- Write-back writes nothing for a beyond-horizon percentile (NMD-2).
- The Jira Release forecast block writes "85%: More than 5 years" on the line of a beyond-horizon percentile
  (NMD-4, AC-03.9).
- Correct the `HowTheRunEnded.RanOutOfDays` comment, which claims no data can reach the horizon (S8), and the
  circle log's "not a forecast" sentence (NMD-13).

## OUT of scope

- Lowering the horizon (slice 04). The Lighthouse-Clients rendering, which is a clients-repo release at
  finalize (NMD-1 option c). The PDF export (Epic #6298 coordinates).

## Learning hypothesis

**Disproves that every forecast surface can carry a non-date without breaking.** The timeline bar, the
predictability chart, pinned closure records and the export table all assume a date. If any of them cannot
carry the marker honestly, slice 04 must not be built until that surface has a design. **Not releasable
alone:** it is released together with slice 04 under Story #6309 (NMD-5). Shipped alone, it would give
released clients a stand-in date about 274 years out.

## Acceptance criteria

AC-03.1 to AC-03.9. Key: **AC-03.1** (the per-percentile rule, circles included), **AC-03.2** (never today),
**AC-03.4** (additive wire), **AC-03.5** (every surface, one wording), **AC-03.9** (the Jira Release block),
**AC-03.8 (production data, manual gate)** the before/after list of Features whose forecast changes on the
restored dev instance; pass when 0 percentiles project to today and the maintainer accepts the list.

## Dependencies

NMD-1 to NMD-4 answered. The DISTILL UI walk-through (UI-1, UI-2) done (NMD-9, NMD-10). Slice 04 follows
it, and the two are released together (NMD-5).

## Effort / reference class

About 1 day; the largest slice. Reference class: forecast-confidence-cap (#5126), one label rule across
every likelihood surface.

## Pre-slice SPIKE

None. DESIGN Q4 (sentinel versus counted field) is a design choice, not an unknown.
