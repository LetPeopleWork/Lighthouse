# RED classification — Story #6218, slices 01–09 (lighthouse-clients)

**Wave**: DISTILL · **Date**: 2026-10-07 · **Clients commit**: `f942462` on clients `main`, parent `2088950`.

Every skipped scenario was un-skipped once (a `.unskipped.test.ts` copy with `it.skip` → `it`, run with
`vitest run --reporter=json`), classified, and the copy deleted. **253 of 253 skipped cases fail, every one
on missing behaviour (`MISSING_FUNCTIONALITY`). None is `IMPORT_ERROR`, `FIXTURE_BROKEN`,
`SETUP_FAILURE` or `WRONG_ASSERTION`.** The 44 active cases (the walking skeleton and the guards) pass on
today's code and must stay green on every slice.

Three kinds of scenario first came back **green** against today's code and were rewritten before this
classification:

- *Unknown shape → generic view* (M1): today everything prints the generic view, so the assertion held
  trivially. Each such scenario now first proves, in the same test, that the recognised shape renders the
  new view; it can only pass once the view exists.
- *Unknown shape → no MCP summary*: the same fix, proving the recognised shape carries a summary first.
- A fixture-only "draws on recorded days" check proved nothing about production code and was removed.

## Failure reasons

| Failure today | Meaning |
|---|---|
| `generic view` | The command prints today's generic indented dump (or today's one-line answer), so the expected lines differ |
| `no summary` | The MCP tool returns one text block; `content[1]` is `null`, or the object answer has no `summary` field |
| `scaffold throws` | `forecastDisplayRules.ts` throws `Not yet implemented -- RED scaffold` |

## Per file

| File (clients repo) | Skipped | Active | Fails because | Class |
|---|---|---|---|---|
| `packages/cli/src/readableOutput.walkingSkeleton.test.ts` | 0 | 1 | — (walking skeleton, GREEN) | — |
| `packages/cli/src/forecastView.test.ts` | 32 | 5 | generic view | MISSING_FUNCTIONALITY |
| `packages/client/src/forecastDisplayRules.parity.test.ts` | 19 | 0 | scaffold throws | MISSING_FUNCTIONALITY (scaffold) |
| `packages/mcp-core/src/forecastSummary.test.ts` | 7 | 1 | no summary | MISSING_FUNCTIONALITY |
| `packages/cli/src/metricsHeadlineView.test.ts` | 12 | 3 | generic view | MISSING_FUNCTIONALITY |
| `packages/cli/src/metricDailyView.test.ts` | 23 | 0 | generic view | MISSING_FUNCTIONALITY |
| `packages/mcp-core/src/metricSummaries.test.ts` | 10 | 1 | no summary | MISSING_FUNCTIONALITY |
| `packages/cli/src/timeInStateView.test.ts` | 7 | 1 | generic view | MISSING_FUNCTIONALITY |
| `packages/mcp-core/src/timeInStateSummary.test.ts` | 2 | 1 | no summary | MISSING_FUNCTIONALITY |
| `packages/cli/src/teamsAndPortfoliosView.test.ts` | 13 | 4 | generic view | MISSING_FUNCTIONALITY |
| `packages/mcp-core/src/ownerSummaries.test.ts` | 12 | 0 | no summary | MISSING_FUNCTIONALITY |
| `packages/mcp-http/src/answerSummaries.e2e.test.ts` | 1 | 0 | no summary (over real HTTP) | MISSING_FUNCTIONALITY |
| `packages/cli/src/deliveriesView.test.ts` | 14 | 2 | generic view | MISSING_FUNCTIONALITY |
| `packages/mcp-core/src/deliverySummaries.test.ts` | 4 | 0 | no summary | MISSING_FUNCTIONALITY |
| `packages/cli/src/featuresView.test.ts` | 11 | 2 | generic view | MISSING_FUNCTIONALITY |
| `packages/mcp-core/src/featureSummaries.test.ts` | 3 | 0 | no summary | MISSING_FUNCTIONALITY |
| `packages/cli/src/writeConfirmations.test.ts` | 15 | 9 | today's one-line answer / generic view | MISSING_FUNCTIONALITY |
| `packages/mcp-core/src/writeSummaries.test.ts` | 5 | 1 | no summary | MISSING_FUNCTIONALITY |
| `packages/cli/src/housekeepingView.test.ts` | 13 | 9 | generic view / `success` / bare version | MISSING_FUNCTIONALITY |
| `packages/mcp-core/src/housekeepingSummaries.test.ts` | 8 | 2 | no summary | MISSING_FUNCTIONALITY |
| `packages/cli/src/prettyForms.test.ts` (KPI-1) | 42 | 2 | pretty output equals the generic view of the same facts | MISSING_FUNCTIONALITY |
| **Total** | **253** | **44** | | |

## Active guards (green today, green on every slice)

They pin what this story must not move, so they are not RED by design: `--json` / `--toon` bytes
(serialised fixture, or a SHA-256 captured on unchanged code for the metrics composite), the reads each
facts-format command makes (no terminology or name read), today's one-line answers under the facts
formats (`Team deleted: 9`, `success`, …), refusals passed through as `category: reason` with exit 1,
MCP errors with no summary block, the `…cumulativeStateTimeCandidates` answer without a summary (M4), and
the KPI-1 completeness checks (every subcommand the help offers and every metric name `lh` accepts is on
the converted list or the unchanged-on-purpose list).

## Suite

`pnpm run ci` (biome check, vitest run, tsc -b, build) in lighthouse-clients: **572 passed, 253 skipped
(825)**, 44 files passed, 6 files skipped; the baseline before DISTILL was 528 passed.
