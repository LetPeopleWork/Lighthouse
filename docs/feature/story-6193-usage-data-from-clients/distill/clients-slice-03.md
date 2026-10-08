# DISTILL — slice 03, lh reports the rest (US-04)

Repository `/storage/repos/lighthouse-clients`, commit `96df092`. Harness as slice 02; every scenario
starts from a stored, live yes, so the question is never in play.

## Files

| File | Pending | Active |
|---|---|---|
| `packages/cli/src/usageDataEvents.test.ts` | 68 | — |

## Scenarios

- Each mapped command reports its web event once, after it succeeded, with source `Cli` and only closed
  fields (7 rows: forecast manual, team and portfolio create / delete / refresh, refinement vote).
- Contract: a Refinement-day vote that made GR-061 Ready is reported as exactly
  `test-support/usageDataContract/batch-lh-sends-for-a-vote-that-made-ready.json`, the bytes Lighthouse's
  own scenario takes in.
- The sizing moment comes from the read before the vote (4 rows); `@error` when that read is refused, the
  vote is refused as today and nothing is reported.
- `lh refinement get` reports one verdict (4 rows), none on a day that is not the Team's Refinement day or
  with no Work Item listed, and one verdict however many Work Items it lists.
- `@error` Refused and failed commands report nothing (delete, forecast, refresh); the commands with no web
  event to mirror report nothing (9 rows, including `lh forecast backtest`, `lh refinement comment`,
  `take-back`, the update, list and get commands, `lh version get`).
- `@kpi` Usage data changes nothing `lh` prints: 9 commands × `--json`, `--toon`, `--pretty`, the same
  bytes as with usage data off; and the same reads from Lighthouse as with usage data off (9 rows).

## DELIVER order

Mapped commands first (the forecast row is already green once slice 02 lands), then the moment, the
verdict, the refused and unmapped rows, then output identity and reads.

## Note for DELIVER

24 of these are claims of absence ("reports nothing", "reads exactly what it reads with usage data off").
They are red today only because the scaffold throws; once `runCliSession` exists they pass on day one.
Un-skip each next to the positive row in the same describe that proves reporting works, never alone.
