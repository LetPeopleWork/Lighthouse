# Epic 5074: Blocked Items — Initial Inputs

**ADO**: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5074
**Status**: Planned (not yet in DISCUSS). No date forecast.
**Origin**: Promoted out of Epic #4144 (More Detailed State Info) — was originally scoped as "slice E: blocked-time history". Moved into its own Epic because the capture mechanism is fundamentally different from the rest of 4144 and warrants its own discovery / design pass.

This document captures only what we already figured out while scoping Epic #4144. It is **not** a DISCUSS artifact — it exists so the carry-over context is not lost when the Epic is eventually picked up.

## Carried-over scope notes (from Epic #4144 scoping)

- **Slice label in 4144 history**: this was "slice E — Blocked-time history".
- **Primary persona candidates**: `flow-coach` + PM (same as 4144's pace-percentiles slice; not yet validated for this Epic specifically).
- **Mechanism is different from 4144.** Whereas 4144 captures `WorkItemStateTransition` from the source-of-truth (Jira / ADO history APIs, with sync-side delta as the CSV/Linear fallback), blocked-time tracking must use its own mechanism. See decision below.

## Carried-over locked decision

| ID | Decision | Why it's already locked |
|---|---|---|
| L1 | **"Blocked" is never pulled from source-of-truth.** Always Lighthouse-side per-sync capture. | "Blocked" is conventionally defined per team — sometimes a tag, sometimes a custom field, sometimes a special state — and varies even within the same connector. There is no portable source-system concept to read. Resolution of the captured timestamps = sync cadence. |

## Carried-over data-foundation note

- Does **not** reuse `WorkItemStateTransition` from `time-in-state-and-staleness`. Needs its own capture (likely a `WorkItemBlockedTransition` or equivalent), populated per-sync by Lighthouse rather than read from the source system.

## What is explicitly NOT decided yet

- Persona scoping (beyond the candidate list above).
- JTBD.
- UX surfaces (per-item badge? cumulative chart? both?).
- Whether the per-team definition of "blocked" is configured in Lighthouse (tag name / custom field name / state name) or auto-detected.
- KPIs, AC, slice split.

These are open and intentionally left for the eventual DISCUSS run.

## Pointers

- Parent context: `docs/feature/epic-4144-more-detailed-state-info/README.md` — the carpaccio split that originally contained this scope.
- Sibling feature (in flight): `docs/feature/time-in-state-and-staleness/` — established the source-of-truth-first pattern that this Epic deliberately diverges from.
