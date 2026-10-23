# Slice 02: Every refresh a user starts forecasts, and a skip stamps "confirmed now" (ADO #6354)

**Goal:** a Team refresh, a Portfolio refresh or a refresh-all that a user started always ends in a full forecast. A
skipped background forecast sets each Feature's forecast "last updated" to the time it was confirmed, leaving the
dates untouched.

## IN scope
- Carry the "user-started" origin from the Team and Portfolio refresh and refresh-all endpoints, through the update
  queue and `TeamDataRefreshed` / `PortfolioUpdater`, to the forecast trigger. A manual request merged into a queued
  automatic one keeps the bypass.
- On a skip, stamp the in-scope Features' forecast "last updated" with the skip time; values and histograms stay
  as they are.
- A stopped or failed round changes no stamp.

## OUT of scope
- Any UI change. `lastUpdated` is not rendered by a web page today, and none is added.
- Coalescing (MD-4).

## Learning hypothesis
- **Disproves** "a button press is a sufficient escape hatch" if a user who presses a Team's refresh still sees a
  forecast that did not recompute (a Task Manager forecast row that ends in seconds after a manual press).
- **Confirms** it if every manual press shows a full "Forecasting Portfolio" run, and after a quiet morning a
  Feature's `lastUpdated` (API or `lh`) reads the last round's time while its dates are unchanged.

## Acceptance criteria
US-02 AC-2.1 to AC-2.4 in `feature-delta.md`.

## Dependencies
Slice 01 (the skip it bypasses and stamps).

## Effort
Half a day to a day. Reference class: adding an origin to a queued update request (Story 5877's lanes), plus a
stamp update on existing forecast rows.

## Dogfood
On the dev instance, press refresh on a Team with nothing changed remotely and watch the forecast run in full. Then
let two background rounds skip and read one Feature's `lastUpdated` through the API.
