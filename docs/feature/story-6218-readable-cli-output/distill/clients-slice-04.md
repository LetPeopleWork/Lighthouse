# DISTILL — slice 04, Time in State (US-04)

Repository `/storage/repos/lighthouse-clients`, commit `f942462`. Harness as slice 01.

## Files

| File | Skipped | Active |
|---|---|---|
| `packages/cli/src/timeInStateView.test.ts` | 7 | 1 |
| `packages/mcp-core/src/timeInStateSummary.test.ts` | 2 | 1 |
| `packages/cli/src/prettyForms.test.ts` row for slice 04 | 1 | — |

## Scenarios

- Rows in workflow order: `State · Total days · Work Items · Completed · Ongoing · Mean · Median`, e.g.
  `To Do 96 18 12 6 5.3 days 4 days`; a null median reads `—`.
- The candidate list is never printed; `--state Review` adds the drill-down; `--item-ids` narrows the
  count ("across n" = candidates, or the `--item-ids` count).
- Error/edge: terms renamed; unknown shape (no workflow order) → generic view.
- MCP: the drill-down summary names the Work Items contributing to the state; the bar summary is a
  string (its sentence is left open).
- Guards: `--state Review --json` SHA-256, reading the three cumulative reads; the candidates answer
  keeps its facts and gains no summary (M4).

## Not pinned (open)

- The MCP bar summary sentence (its count needs the candidates read).
- A Time in State answer with no states at all.
