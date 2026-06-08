# Slice 01 - Walking Skeleton: one named cycle time end-to-end onto the scatterplot

**Type:** vertical (walking skeleton) | **Est:** ~1 day | **Stories:** US-01

## Learning hypothesis

We can compute an ordered-boundary named cycle time by GENERALISING the existing
`StartedDate`/`ClosedDate` derivation over `AllStates` (not a parallel engine), serve it through a
NEW per-definition read endpoint, and render it on the scatterplot via a selector - proving the whole
data path (definition -> compute -> endpoint -> chart) before any CRUD UI exists.

## What ships

- Backend: a named-cycle-time computation that, given `{ startState, endState }`, derives each closed
  item's duration via first-transition-into-start-or-later -> first-transition-into-end-or-later over
  `AllStates` order (reusing the started/closed derivation pattern; D1/D2).
- A NEW read endpoint returning per-definition scatter data + 50/70/85/95 percentiles for a single
  definition (Team scope), version-gated in the clients registry (see feature-delta cross-cutting).
- Frontend: a cycle-time selector on the Cycle Time Scatterplot listing "Default" + ONE seeded/hard-
  coded definition (e.g. "Concept to Cash" = Planned->Done); selecting it re-plots dots and recomputes
  percentile lines. Premium-gated via `useRbac()`.

## IN scope

- Single hard-coded/seeded definition (no persistence, no CRUD) - just enough to drive the chart.
- Team scope only.
- Sparse/empty series low-sample state (D9) for the named series.

## OUT of scope

- Settings CRUD (Slice 02), invalid-on-removal (Slice 03), cumulative scope switch (Slice 04),
  Portfolio (Slice 05), re-entry edge beyond first-crossing (D2 covered, nothing more).

## Production-data AC

- Given Team Phoenix has closed items that crossed Planned then Done, when Priya selects "Concept to
  Cash" on the scatterplot, then each dot's Y is its Planned->Done duration and the P50/70/85/95 lines
  recompute over that series.
- Given fewer than the minimum items crossed both boundaries, when Priya selects the definition, then
  the chart shows an explicit low-sample state, not percentile lines on one or two dots.

## Taste tests

- Walking skeleton: touches compute + endpoint + chart end-to-end. PASS.
- Value-bearing (not @infrastructure): a delivery lead sees a real custom-window scatterplot. PASS.
- Demoable in one session. PASS.
