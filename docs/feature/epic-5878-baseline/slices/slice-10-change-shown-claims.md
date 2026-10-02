# Slice 10 — Change which claims a report shows (cancellable)

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-10 (ADO #6168) · **Estimate**: ~½d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

An editor shows or hides claims on an existing report from the values frozen at creation, with nothing recomputed.

## IN

- "Edit claims" on the report, Write only; ticks reveal rows from frozen values; at least one stays selected.
- Claims registered after creation read "Not captured for this report" and cannot be ticked (D27).

## OUT

Adding new claims to an old report by recomputing Then (never).

## Learning hypothesis

**This disproves "freezing every claim pays off"** (DV-6) if nobody changes a selection within a month of release.
Freezing everything stays (it is cheap and already built), but later templates need not extend it.

## Data and dogfood moment

- Dogfood: tick {Throughput} on a report where it was unticked, after the sponsor asks about it.

## Acceptance criteria

AC-10.1 … AC-10.3 in `feature-delta.md` (US-10).

## Dependencies

Slice 04. Cancellable: the maintainer may move it to a later Story set (Scope Assessment).
