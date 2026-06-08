# Slice 04 - Cumulative-time-per-state chart scope switch to a named window (D6b)

**Type:** vertical | **Est:** ~1 day | **Stories:** US-04

## Learning hypothesis

The cumulative-time-per-state chart can be scoped to a named cycle time's window (only states inside
the boundary window contribute) using the same `{ startState, endState }` over `AllStates`, so the
delivery lead can see WHICH states inside the custom window consumed the time - reusing the chart's
existing per-state aggregation rather than a new computation.

## What ships

- Backend: extend the cumulative-time-per-state data path to accept an optional cycle-time definition
  id and scope the per-state bars to that window's span (states between start and end in `AllStates`).
- Frontend: a "scope to cycle time" switch on the cumulative-time-per-state chart (Team); off = today's
  behavior unchanged; on with a selected named definition = bars recompute over the window. Invalid
  definitions (Slice 03) appear disabled here too. Premium-gated via `useRbac()`.

## IN scope

- Team-scope cumulative chart scoping to a named window; off-state byte-identical to today.

## OUT of scope

- Portfolio (Slice 05); scatterplot changes (Slices 01-02 already done).

## Production-data AC

- Given Team Phoenix has a "Concept to Cash" definition, when Priya turns on the scope switch and
  selects it, then the cumulative bars recompute over the Planned->Done span and the "Planned" and
  "Validation" state bars are visible as the dominant contributors.
- Given the scope switch is off, when Priya views the chart, then it behaves exactly as it does today.
- Given the selected definition is invalid (Slice 03), when Priya opens the switch's selector, then it
  is disabled with a warning and the chart stays unscoped.

## Taste tests

- Value-bearing: the second MVP surface (D6b) - completes the "where inside the window?" read. PASS.
- Right-sized: one chart, one switch, 3 scenarios. PASS.
