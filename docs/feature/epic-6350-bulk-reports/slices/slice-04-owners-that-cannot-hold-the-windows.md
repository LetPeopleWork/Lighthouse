# Slice 04 — The owner step says which owners cannot take the report

**Feature**: epic-6350-bulk-reports · **Epic**: #6350 · **Story**: US-04 (ADO: to be created) · **Estimate**: 1d ·
**Tier**: Premium · **Ships alone**: yes (on 02b and 03) ·
`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`

## Goal

Before Create, the owner step shows which Teams and Portfolios cannot take the report — kept history too short for the
windows, or a chosen metric not captured — with the reason and where to fix it, so the user decides then rather than
reading reasons afterwards (D1's "limit picks where it can").

## IN

- Each owner whose kept history cannot hold Then or Now: shown, not pickable, with "Kept history starts {date}" and a
  link to its settings (Closed Items Cutoff) — D13, LOCKED (maintainer, 2026-10-27; Q4).
- Each owner that does not capture a chosen metric: shown, not pickable, with the reason — D13 extension, PROPOSED —
  confirm at DISTILL sketch walk (Q7). The Metrics step keeps offering every metric.
- The Create count excludes them; going back and changing windows or metrics re-evaluates.
- Same bounds as the server (as `reportWindowCheck.ts` today); an unknown history start leaves the owner pickable and
  the server decides (02a's result).
- While the owners' limits load, Create is not available; if they cannot be loaded, owners stay pickable and the step
  says their limits could not be checked.
- Raising an owner's cutoff takes effect only after its next update has brought the older Work Items back.

## OUT

The single wizard's limits (Story 6347, Removed; unchanged) · changing an owner's cutoff from the wizard.

## Learning hypothesis

**Disproves "saying it before Create beats reasons after"** if dogfood runs still end with skipped owners the user did
not expect.

## Data and dogfood moment

Dev instance: set one Team's Closed Items Cutoff to 90 days and run the quarter comparison. Demo: a Team with a short
cutoff in the test.

## DESIGN must answer

How each owner's earliest kept day and captured metrics reach the browser; their loading and failure looks inside the
step.

## DISTILL must sketch first

The unpickable rows (history and metric copy, link) and the count.

## Acceptance criteria

AC-4.1 … AC-4.7 in `feature-delta.md` (US-04).

## Dependencies

Slices 02b and 03 (so both owner kinds are covered at once).
