# Slice 02: A skipped forecast reads as confirmed now (ADO #6354)

**Goal:** a skipped forecast sets the forecast "last updated" of every Feature it covers to the time it was confirmed,
leaving the dates untouched, so a reader of the Features API sees how fresh the forecast is.

*(Re-cut at the DISTILL review gate: the button forgets this slice used to carry moved to slice 01, so it is the
stamp alone. The file name is kept for the links to it.)*

## IN scope
- On a skip, stamp the stored completion and start forecasts of every Feature the forecast covers with the skip time,
  including Features of another Portfolio that this Portfolio's Teams work on; values and histograms stay as they are
  (DDD-6).
- A stopped or failed round stamps nothing.

## OUT of scope
- Any UI change. `lastUpdated` of a Feature is not rendered by a web page today, and the `lh` clients read it only for
  Teams and Portfolios; none is added. The change is visible through `GET /api/latest/features` alone.
- Coalescing (MD-4).

## Learning hypothesis
- **Disproves** "a confirmed-now time is enough to tell a fresh forecast from a stale one" if, after a quiet morning,
  a Feature's `lastUpdated` reads a time other than the last round's while its forecast was skipped.
- **Confirms** it if a Feature's `lastUpdated` reads the last round's time while its dates are unchanged.

## Acceptance criteria
US-02 AC-2.2 to AC-2.4 in `feature-delta.md`.

## Dependencies
Slice 01, finished (the skip it stamps).

## Effort
About half a day. Reference class: one set-based update on existing forecast rows, plus its relational test.

## Dogfood
On the dev instance, let two background rounds skip, then read one Feature's `lastUpdated` and `forecasts` through
`GET /api/latest/features`: the time is the second round's, the dates are those of the last computed forecast.
