# Slice 03 — A Feature linked to several issues above it gets one parent, the oldest link's

**Feature**: story-6301-only-features-as-parent-candidates · **ADO**: #6301 · **Story**: US-03 · **Estimate**: ~5h
**Reference class**: slice 02's pick, applied at the other grain with narrowing switched off (maintainer Q3).

## Goal

At Portfolio grain, a Feature whose links name two or more issues above it gets one parent by the oldest-link
rule, with nothing set aside first, and the Portfolio's refresh row reports it as placed.

## IN scope

- The placing step on both Portfolio fetches (full and delta), narrowing off (AC-3.1).
- Created-date fallback uses stored parent Features where they exist; otherwise lowest key (AC-3.2).
- Placed figure on the Portfolio refresh row, full and delta (AC-3.3).
- The placed line names item, pick, rule and every candidate, so a Portfolio reading links in both directions
  (#6028 D7) is still diagnosable from the log (AC-3.4).
- The parent sweep (`GetParentFeaturesDetails`): through the same step if DESIGN keeps the default (yes); its
  "warns but does not count" behaviour kept or restated (AC-3.5).

## OUT of scope

- Narrowing at this grain (D5): the level above is never a Portfolio Feature.
- Re-resolution when a parent above is stored later (D6).

## Learning hypothesis

**Disproves, if it fails**: that the oldest-link pick is safe at Portfolio grain without narrowing, i.e. that
a Portfolio pointed at the right (upward) phrase rarely sees a non-hierarchy issue win.

**The measurement**: on the dogfood Portfolio pointed at "Blocks", count placed Features whose pick is not
a stored parent Feature, over placed Features. Pre-registered: **more than 1 in 5** picks landing on a
non-hierarchy issue sends Portfolio-grain narrowing back to the maintainer as a question (it was declined
under Q3 on reasoning, not data). Record the number and denominator here. Directional only: link use on
the demo instance is sparse.

**Confirms, if it succeeds**: Q3's "tie-break only" holds, and every Feature rolls up to one parent, stably.

## Acceptance criteria

AC-3.1 through AC-3.5 in `feature-delta.md` (US-03).

## UI

None new. The Portfolio entity in Refresh History shows the placed figure; label term per open point S-3
(Features term on Portfolio rows) in `feature-delta.md`.

## Data

- **Dogfood**: `LGHTHSDMO`, Portfolio Parent Override Field on "Blocks"; link one Feature to two issues above
  it. Then, deliberately, point the Portfolio at the type's own name so a Feature sees its children too:
  the placed line names every candidate (the D7 diagnosis, CA-3).
- **Production**: Steve's customer, if its Portfolios read the same link type.

## Dependencies

Slice 02 (the pick and link dates).

## Pre-slice SPIKE

No. Slice 02's spike settles the changelog shape for both grains.

## Dogfood moment

Same day: run the D7 misconfiguration on purpose and confirm, from the Task Manager line alone, that you can
tell the Portfolio is reading downward links.

## Docs at finalize

`docs/portfolios/edit.md:275`: the diagnosis changes from "refused" to "placed under one of its links,
named with every candidate"; say what a downward-reading Portfolio looks like now.
