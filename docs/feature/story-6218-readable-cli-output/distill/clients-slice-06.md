# DISTILL — slice 06, Deliveries (US-06)

Repository `/storage/repos/lighthouse-clients`, commit `f942462`. Harness as slice 01.

## Files

| File | Skipped | Active |
|---|---|---|
| `packages/cli/src/deliveriesView.test.ts` | 14 | 2 |
| `packages/mcp-core/src/deliverySummaries.test.ts` | 4 | 0 |
| `packages/cli/src/prettyForms.test.ts` rows for slice 06 | 3 | — |

## Scenarios

- Delivery list, exact table: Q4 Release `78%`, Pilot Launch `>95%`, Beta Drop `Overdue`, Spring
  Rollout `Not enough data` with `—`.
- Error/edge: Cannot forecast ×2; a finished Delivery on thin history reads `100%`; an older server
  without `isOverdue`; no 85% date → `—`; empty list → `No Deliveries` (M9); Portfolio name refused →
  `Portfolio [id: 2] · Deliveries`; renamed (`Ocean Explorer · Releases`); time zone ×2; unknown shape.
- Delivery metrics: heading `Delivery [id: 11] · Delivery Date Tue 15 Dec 2026 · recorded since Tue 15
  Sep 2026`, one row per recorded day; `--detail epics` adds `On Tue 6 Oct 2026`, the Feature table and
  the chance table.
- MCP: `4 Deliveries` / `1 Delivery`; the summarised metrics gain a second block with the heading; the
  detailed answer gains `summary`.
- Guards: list `--json` / `--toon`; metrics summarised and detailed `--json`.

## Decided in DISTILL

- The empty list mirrors `DeliveriesChips.tsx`: heading, then `No Deliveries` (M9).
- The metrics heading comes from the full history; the summarised rows carry no dates.

## Not pinned (open)

- Which wins when a Delivery is both overdue and cannot be forecast (the web shows Overdue as a separate
  chip; the sketch puts it in the Likelihood cell).
- Delivery metrics for a Delivery with no recorded days.
- The MCP count for an empty list (`0 Deliveries` or `No Deliveries`).
