# Slice 02 — The Feature table says when each one starts

**Feature**: epic-6033-forecasted-start-dates · **ADO**: to create · **Story**: US-02 · **Estimate**: ~3h
**Reference class**: the completion forecast column already in the Feature table. Same data shape, same
percentile presentation, same terminology entries, same empty state. This slice adds a second column
beside it, not a second way of rendering a forecast.

## Goal

Someone looking at a Portfolio's Features can see when each one is expected to be picked up, without
reading the completion dates of everything above it in the order.

## IN scope

- A Forecasted Start column in the Feature table, on Portfolio and Team detail.
- Not-started Features render P50/P70/P85/P95, following the completion column's presentation (D9).
- Started Features render one date, visibly distinguished as observed rather than forecast (D5). The
  distinction has to be legible without hovering — a reader scanning the column must not mistake an
  observed date for a P50.
- The existing empty state, reused, for a Feature with no start forecast. No new empty state.
- Terminology entries for the column header, per the seeded defaults.
- Vitest coverage including the no-licence case (AC-2.4), plus one Playwright walking skeleton through
  the existing page object.

## OUT of scope

- The timeline. Slice 04.
- Any backend change. Slice 01 shipped the data; if this slice needs a backend change, slice 01 was
  incomplete and the change belongs there.
- Sorting or filtering on the new column. Not asked for, and the completion column does not have it
  either.
- Any change to the completion column.

## Learning hypothesis

**Disproves, if it fails**: that a start date reads as useful next to a completion date — the assumption
underneath the whole Epic's UI half.

Two ways it disappoints rather than fails outright:

1. **Four percentiles times two columns is eight dates per row.** The completion column already carries
   four. Doubling that may be the point at which the table stops being readable and starts being a
   spreadsheet. If so, the answer is a default-collapsed presentation showing one percentile with the
   rest on demand — not dropping to a single percentile, which is what D9 exists to prevent.
2. **The numbers may look obviously wrong.** This is the first slice where a human reads the start
   dates in context, in board order, beside the completion dates. D6's optimism — a not-started Feature
   forecast to start today while the WIP it needs is spent lower down — is invisible in JSON and
   conspicuous in a column. That is the point of shipping this before the write-back.

**Confirms, if it succeeds**: the data shape from slice 01 is what the UI wants, and the timeline is
rendering rather than modelling.

## Acceptance criteria

AC-2.1 through AC-2.5 in `feature-delta.md`.

## Dependencies

Slice 01. Nothing else.

## Pre-slice SPIKE

**No.** The column is a variation on one that exists.

## Effort

~3h.

## Dogfood moment

Same day: open the dev instance Portfolio and read the column in board order, top to bottom. The
question to answer out loud before calling this done is whether the start dates are *believable* — not
whether they render. If a Feature nobody has touched says it starts today while three others are in
flight, that is D6 showing itself, and it should be recorded in the ADO follow-up with the actual
screenshot rather than remembered as a theoretical concern.
