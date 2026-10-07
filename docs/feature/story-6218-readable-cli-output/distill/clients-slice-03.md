# DISTILL — slice 03, one metric, every day (US-03)

Repository `/storage/repos/lighthouse-clients`, commit `f942462`. Harness as slice 01.

## Files

| File | Skipped | Active |
|---|---|---|
| `packages/cli/src/metricDailyView.test.ts` | 23 | 0 |
| `packages/mcp-core/src/metricSummaries.test.ts` | 10 | 1 |
| `packages/cli/src/prettyForms.test.ts` rows for slice 03 | 10 | — |

## Scenarios

- Throughput day table; each of the 10 metric names gets its heading, sentence and columns
  (`predictabilityScore` has the score and its explanation, no table).
- WIP Work Item rows; cycle time closed-item rows; predictability explanation verbatim.
- Error/edge: empty history ×2; several names printed in the order given ×2; `--definition-id 4` names
  the cycle time definition (`Lead Time Percentiles…`); terms renamed; unknown shape; time zone ×2.
- MCP: list tools ×5 keep the facts block byte-identical and add a second block; empty percentiles keep
  `team percentilesOverTime: []`; object tools ×3 gain `summary`; blocked history in an unknown shape →
  no summary.

## Decided in DISTILL

- `--definition-id` reads the definition's name through a new client read, `getTeamSettings`
  (`GET /teams/{id}/settings`): `TeamDto` carries no cycle time definitions. Raised upstream (it is a
  third read on that one form, against "≤ 2 extra reads").

## Not pinned (open)

- The `--metrics predictabilityScore` percentile table: the composite keeps only `{score}`, so drawing
  it would change `--json`.
- `--metrics <name>` when that section's read is refused, outside the headline.
- The MCP summary for the work item age tool: no sentence settled.
