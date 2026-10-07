# DISTILL — slice 05, Teams and Portfolios (US-05)

Repository `/storage/repos/lighthouse-clients`, commit `f942462`. Harness as slice 01.

## Files

| File | Skipped | Active |
|---|---|---|
| `packages/cli/src/teamsAndPortfoliosView.test.ts` | 13 | 4 |
| `packages/mcp-core/src/ownerSummaries.test.ts` | 12 | 0 |
| `packages/mcp-http/src/answerSummaries.e2e.test.ts` | 1 | 0 |
| `packages/cli/src/prettyForms.test.ts` rows for slice 05 | 4 | — |

## Scenarios

- Team list (first five lines; reads only `listTeams` and terminology); Last Updated in the reader's
  zone (Adak and Kiritimati both pinned); `—` for a Team never updated; an item without a name → generic
  view.
- Team get, exact nine lines; `Not set` ×3 for a Team without SLE / WIP; `Feature WIP: 1 Feature`;
  terms renamed.
- Portfolio list with its hint line and no Delivery read; Portfolio get, exact seven lines;
  `Feature WIP: Not set` for a Portfolio without involved Teams.
- MCP: count sentences `7 Teams`, `1 Team`, `7 Squads`, `5 Portfolios` (M3); seeded words when
  terminology fails; unknown shape → none; get tools gain `summary`; descriptions mention it.
- mcp-http e2e: a `node:http` fake Lighthouse; `lighthouse_team_list` answers two blocks, the second
  `summary: 1 Team`.
- Guards: `--json` / `--toon` for the four forms.

## Decided in DISTILL

- The Tags line is left out when Lighthouse sends no tags; the list's Tags cell stays empty (the server
  sends none today).
- A Portfolio's Feature WIP counts its involved Teams, as the sketch shows.
