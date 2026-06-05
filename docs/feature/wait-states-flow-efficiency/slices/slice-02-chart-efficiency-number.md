# Slice 02 — Per-chart flow-efficiency number (aggregate + per-item via picker)

**Story**: US-02 · **Job**: `job-spot-flow-efficiency-waste` · **Persona**: delivery-lead-rte (Priya Nair) / flow-coach

## Goal (one line)

Show a flow-efficiency number on the Cumulative Time per State chart — aggregate for the in-scope set, and
recomputed for an individual item when the existing US-05 picker narrows to one item.

## Learning hypothesis

Surfacing the number where per-state time already lives (and making it follow the picker) gives leads/coaches the
"efficiency for this item if we filter" answer the work item explicitly asks for — without a new screen or export.

## In scope

- Flow-efficiency figure rendered on the cumulative chart for the current in-scope set (picker cleared).
- Number recomputes over the US-05 picker selection (one item → that item's efficiency), per D5.
- Suppress the number when no wait states configured (D3); "no data in scope" when zero Doing-time (D4).
- Reuse existing cumulative endpoints + picker. Efficiency value additive on `cumulativeStateTime` response OR
  derived client-side from existing bars + `WaitStates` (D8 — DESIGN picks).

## Out of scope

Overview tile (03), wait-bar highlight (04). The chart RAG and the tile do NOT follow the picker (D5/D18).

## Done when

- US-02 ACs pass; picker-cleared number == (future) tile scope; one item → that item's efficiency; RAG unchanged
  by picker; suppression (D3) and "no data in scope" (D4) verified. Build clean; ≥80% mutation.

## Dependencies

Slice 01 (efficiency computation + `WaitStates`). Existing `state-time-cumulative-view` chart + US-05 picker (shipped).

## Note

Per-item-usage KPI is guarded by the Epic 5015 self-hosted telemetry gap — tracked qualitatively until telemetry lands.
