# Slice 04 — Wait-bar colour-highlight on the cumulative chart

**Story**: US-04 · **Job**: `job-spot-flow-efficiency-waste` · **Persona**: delivery-lead-rte (Priya Nair) / flow-coach

## Goal (one line)

Render wait-state bars on the Cumulative Time per State chart in a treatment visually distinct from active-state
bars, so the idle waste is visible at a glance.

## Learning hypothesis

Making wait-state bars visually pop turns "which states are the waste?" from a tooltip hunt into a glance,
sharpening the constraint conversation and getting the highlighted chart into shared decks.

## In scope

- Wait-state bars (states in `WaitStates`) rendered in a distinct colour family with a legend entry (D6).
- Composes with the existing completed/ongoing segment split (solid base / hatched top) and remains legible together.
- Colour-blind-safe: distinction conveyed by legend label and/or pattern/icon, not colour alone (reuse the
  cumulative chart's existing colour-blind conventions).
- No wait states → nothing highlighted, chart unchanged (no regression).
- Highlight reads from the SAME `WaitStates` source as the efficiency math (single source — registry HIGH-risk item).

## Out of scope

Any change to throughput/forecast/cycle-time numbers (D9 — labelling overlay only).

## Done when

- US-04 ACs pass; wait bars distinct + legend; composes with segments; colour-blind-safe; no-wait-states
  no-regression; highlight + math read the same list (integration test). Build clean; ≥80% mutation (mostly
  presentational — justify equivalent/presentational survivors per project convention).

## Dependencies

Slice 01 (`WaitStates` source). Independent of slices 02/03. Existing `state-time-cumulative-view` chart (shipped).
