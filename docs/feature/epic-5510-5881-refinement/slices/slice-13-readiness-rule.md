# Slice 13 — Votes make Work Items Ready; the verdict counts them

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E3 Sizing votes (#5510) · **Story**: US-13 ·
**Estimate**: ~1d · **Tier**: Community (veto and rule override included, D25)

## Goal

A Team admin sets readiness: minimum Yes votes (at least 1), minimum voters, and an optional veto on x No and/or
"Yes, but…" votes. Defaults: 3 Yes, minimum voters 3 (never below minimum Yes), veto off (DD-7, DD-21). Each row
then reads Ready, "2 more Yes needed" or "Needs discussion", and the heading counts the Work Items votes make Ready.
**Ships standalone** (DD-22): it runs before slices 03/04/05. When 03 lands, state-stage readiness joins the count;
when 05 lands, the verdict uses it (AC-5.7).

## IN

- The readiness setting in Settings → Refinement, with expand-only storage.
- A status per row. The missing votes are named so they can be raised in the Team's rituals (D21).
- A "n ready by votes" count in the tab heading. The verdict and the line are **not** in this slice; they read this
  count when slice 05 / 06 land.
- The usage-data "readiness reached" event (K5), as designed in DEVOPS. Its before/on-Refinement-day property can only
  be filled once a cadence exists (slice 04); DEVOPS decides whether to emit it without the property until then.

## OUT

Push or reminders (D21), writing readiness back to the tracker (D9).

## Learning hypothesis

**This disproves "votes can stand in for 'ready'"** if, on the dogfood Team (checked once slice 03 lands), the
vote-ready Work Items and the state-ready Work Items disagree a lot — for example, many Work Items in `Next` collect No votes. That would mean the
DD-5 order is wrong and has to be re-decided with the maintainer.

## Data and dogfood moment

- Demo: GR-073 has Yes votes from Jonas and Mo Okafor and "Yes, but…" from Ana, so it is Ready at the default of 3.
  GR-054 has Ana's No, so it needs discussion when a veto of 1 No is set.
- Dogfood: compare vote-ready and state-ready at the next Refinement.

## Acceptance criteria

AC-13.1 … AC-13.4 (US-13).

## Dependencies

Slice 11 only. Not slice 05 — the verdict picks this count up when 05 lands (DD-22).
