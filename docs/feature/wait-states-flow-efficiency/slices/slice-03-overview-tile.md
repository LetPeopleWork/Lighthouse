# Slice 03 — Flow Efficiency overview tile

**Story**: US-03 · **Job**: `job-spot-flow-efficiency-waste` · **Persona**: delivery-lead-rte (Priya Nair)

## Goal (one line)

A Flow Efficiency tile in the Flow Overview row showing the team's aggregate efficiency percentage with a RAG colour.

## Learning hypothesis

A single efficiency percentage in the overview KPI row is the glance-level signal that prompts leads to open the
cumulative chart and investigate which wait states are dragging it.

## In scope

- New `flowEfficiency` widget in `categoryMetadata.ts` (`flow-overview`, `small`) on team + portfolio detail
  pages (D7).
- Aggregate efficiency percentage (whole in-scope set; NOT affected by the chart picker, D5).
- RAG mapping (D10): `act` < 40%, `observe` 40–60%, `sustain` ≥ 60% — in `widgetInfoMetadata.ts` `statusGuidance`.
- "Not configured" state (D3, never 100%); "no data in scope" (D4, no division error).
- New small efficiency read endpoint OR folded into overview payload (D8 — DESIGN picks); `trendPolicy` chosen in DESIGN.

## Out of scope

Wait-bar highlight (04). The tile is always whole-set (does not follow the chart picker).

## Done when

- US-03 ACs pass; tile renders with correct RAG; "not configured" and "no data in scope" states verified; no
  regression in `flow-overview`. Build clean; ≥80% mutation.

## Dependencies

Slice 01 (efficiency computation). Independent of slice 02 (can ship before or after it).

## Note

Tile-view KPI guarded by Epic 5015 self-hosted telemetry gap.
