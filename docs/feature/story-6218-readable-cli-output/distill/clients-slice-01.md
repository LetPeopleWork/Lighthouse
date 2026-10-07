# DISTILL — slice 01, forecasts (US-01)

Repository `/storage/repos/lighthouse-clients`, commit `f942462`. Driving ports: `runCliCommand` through
`packages/cli/test-support/cliHarness.ts` (`aLighthouse`), `createMcpCoreRuntime(...).callTool` through
`packages/mcp-core/test-support/mcpHarness.ts` (`anAssistantOn`). Only Lighthouse is faked: a stub client
answers each read from a table and records every read by method name.

## Files

| File | Skipped | Active |
|---|---|---|
| `packages/cli/src/forecastView.test.ts` | 32 | 5 |
| `packages/client/src/forecastDisplayRules.parity.test.ts` | 19 | 0 |
| `packages/mcp-core/src/forecastSummary.test.ts` | 7 | 1 |
| `packages/cli/src/prettyForms.test.ts` rows for slice 01 | 2 | — |

Scaffold: `packages/client/src/forecastDisplayRules.ts` (`levelOf`, `formatLikelihood`, both throw;
`__SCAFFOLD__ = true`). Not exported from the package index yet; DELIVER exports it when it goes green.

## Scenarios

- Manual forecast, exact lines; forecast level per chance (8 rows: ≤50 Risky, ≤70 Realistic, ≤85
  Confident, else Certain); likelihood wording (5 rows, `>95%` only with work remaining).
- Error/edge: Cannot forecast; not enough data; `hasSufficientData` absent; only `--remaining`; only
  `--target-date`; filtered heading; terms renamed (KPI-5); terminology refused → seeded words; Team name
  refused → `Team [id: 3] · …`; unknown shape ×2 → generic view (M1); reader in two time zones.
- Backtest: exact lines; where the actual result sits among the percentile rows (30 first, 19 between,
  10 last; a tie goes after the equal row); Team name refused.
- Parity with the web (DSN-7): 10 `levelOf` rows, 9 `formatLikelihood` rows.
- MCP: `summary` field on the manual and backtest answers; name refused; renamed; unknown shape → no
  summary; tool descriptions mention `summary`.
- Guards: `--json` / `--toon` byte-identical for manual and backtest, reads exactly
  `runManualForecast` / `runBacktest`; refusals pass through.

## DELIVER order

1. Un-skip the parity tables, implement `forecastDisplayRules.ts` (removes the scaffold).
2. Un-skip `forecastView.test.ts` top to bottom, then the slice-01 rows of `prettyForms.test.ts`.
3. Un-skip `forecastSummary.test.ts`.

## Not pinned (open)

The heading wording when only `--remaining` or only `--target-date` is given: the scenarios pin the
lines that follow, not the heading.
