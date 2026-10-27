# Slice 04 — The owner step says which owners cannot take the report

**Feature**: epic-6350-bulk-reports · **Epic**: #6350 · **Story**: US-04 (ADO: to be created) · **Estimate**: ~¾d (rev. after DESIGN, 2026-10-27: was 1d with the metric half) ·
**Tier**: Premium · **Ships alone**: yes (on 02b and 03) ·
`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`

## Goal

Before Create, the owner step shows which Teams and Portfolios cannot take the report — kept history too short for the
windows — with the reason and where to fix it, so the user decides then rather than
reading reasons afterwards (D1's "limit picks where it can").

## IN

- Each owner whose kept history cannot hold Then or Now: shown, not pickable, with "Kept history starts {date}" and a
  link to its settings (Closed Items Cutoff) — D13, LOCKED (maintainer, 2026-10-27; Q4).
- The Create count excludes them; going back and changing windows re-evaluates.
- Same bounds as the server (as `reportWindowCheck.ts` today); the server still decides (02a's result).
- Owners and their history starts arrive in one read (DESIGN DDD-3): while it loads, the loading look and no Create;
  if it fails, the could-not-load look with Retry (AC-2b.3's look). No partial state (rev. after DESIGN, 2026-10-27).
- Raising an owner's cutoff takes effect only after its next update has brought the older Work Items back.

## OUT

The single wizard's limits (Story 6347, Removed; unchanged) · changing an owner's cutoff from the wizard · owners that
do not capture a chosen metric (dropped after DESIGN, 2026-10-27, maintainer MQ-3 a: metrics are captured per owner
kind and today every metric applies to both kinds; re-add when a metric applies to one kind only).

## Learning hypothesis

**Disproves "saying it before Create beats reasons after"** if dogfood runs still end with skipped owners the user did
not expect.

## Data and dogfood moment

Dev instance: set one Team's Closed Items Cutoff to 90 days and run the quarter comparison. Demo: a Team with a short
cutoff in the test.

## DESIGN must answer

How each owner's earliest kept day reaches the browser; its loading and failure looks inside the step. *Answered*:
the creation-options read (DESIGN DDD-3).

## DISTILL must sketch first

The unpickable rows (history copy, link) and the count.

## Acceptance criteria

AC-4.1 and AC-4.3 … AC-4.7 in `feature-delta.md` (US-04); AC-4.2 dropped after DESIGN.

## Dependencies

Slices 02b and 03 (so both owner kinds are covered at once).
