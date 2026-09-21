# Slice 07 — The timeline says which Features are done and which are late

**Feature**: epic-6033-forecasted-start-dates · **ADO**: #6067 · **Story**: US-07 · **Estimate**: ~5h
**Severable.** Dropping it leaves slices 04, 05 and 06 whole; AC-7.12 says so as a criterion rather than
a hope. Independent of 05 and 06 — any of the three can go.

**Timeline only.** The Delivery's Features grid already answers "is this one late?" with
`FeatureLikelihoodChip`, and a second answer beside it would be free to disagree with the first (D7-6).

**Reference class**: the third thing drawn on slice 04's chart, after 05's links and 06's lanes. Unlike
either, it is a statement about the *target date* — the one thing on this chart that was marked and then
never referred to again.

## Goal

A reader opening a Delivery timeline can see, without measuring anything against the tinted target
column, which Features are finished and which two kinds of late the rest are.

## IN scope

- Three statuses on a Feature's own bar, as a coloured **edge**, not a fill (D7-1): done, forecast to
  finish after the target, forecast not to start until after it.
- The test is the drawn bar against the target date at the selected probability (D7-2), so the statuses
  move when the probability does.
- Precedence: done over late, start-late over finish-late, on-track carries nothing (D7-3, D7-4, D7-5).
- A key above the chart naming each colour, in the slot and form the Team key already uses.
- A **Show status** switch, default off, absent when no bar would carry one.
- A **Show warnings** switch, default off, hiding every bar mark — symbol and hover sentences alike
  (D7-9). *Severable within the slice*: it is the story's "while we are at it" rider and touches nothing
  else here.

## OUT of scope

- **Removing or re-ordering a late Feature.** Declined, not deferred (D7-12). This slice indicates.
- **Any surface but the timeline** (D7-6). The Features grid keeps its chip; the Portfolio Feature table
  has no target date to be late against.
- **Any backend change.** Everything is computable from what the tab already receives.
- **Status on a Team sub-lane** (D7-7). The statement is about the Feature.
- Re-deciding `ForecastLevel`'s four-level ladder. This slice borrows three of its colours, not its
  thresholds.

## Learning hypothesis

**Disproves, if it fails**: that a coloured edge can carry a status at the bar heights this chart draws.

Two ways it disappoints:

1. **The edge may be invisible.** These bars are a few pixels tall and the fill beneath one may be any of
   fourteen Team colours or the library's default. An edge that reads cleanly in a mock-up and vanishes
   on a twenty-row chart is the whole slice failing at its first dogfood — and the honest response is the
   story's own alternative, hatching, not a thicker edge.
2. **The colours may collide with the ones already on screen.** `getColorMapForKeys` claims to avoid the
   red spectrum and contains `#E57373` Soft Red, `#F2A65A` Warm Orange and three greens. With Show Teams
   on, a status edge in `#f44336` sits next to a Team fill in `#E57373`. If the reader cannot tell the
   two reds apart, the two switches cannot be on at once — which is exactly the coexistence D7-1 chose
   the edge for.

**Confirms, if it succeeds**: the target date stops being a column nobody reads against. It was marked in
slice 04 and has been decoration ever since; this is the first thing on the chart that compares anything
to it.

## Acceptance criteria

AC-7.1 through AC-7.12 in `feature-delta.md`.

## Dependencies

Slice 04, entirely — there is no chart without it. Nothing else: no backend, no slice 05, no slice 06.
With slice 06 present it must coexist with the Team fills, which is what D7-1 exists to guarantee and
AC-7.10 exists to check.

## Pre-slice SPIKE

**No, but do not write the switches first.** Draw one chart with three edges over both the default fill
and a pastel Team fill, in both themes, and look at it. Risk 1 is decided there, in about twenty minutes,
before any of the toggle machinery is worth writing. If the edge loses, D7-1 falls to hatching and the
rest of the slice is unchanged.

> _(to be filled)_

## Effort

~5h. Status rule ~1h, the edge ~1h, key ~0.5h, two switches and their store ~1h, tests ~1.5h.

## Dogfood moment

Same day: open a Delivery with a target date the plan does not fit, turn on Show status, and move the
probability from 70 to 95. The demo is not the colours appearing — it is whether the reader can say which
Feature to talk to first, and whether they say a different name at 95 than at 70. If the same bar is red
at every probability, the colour is restating the board order and not the forecast.
