# DISTILL — slice 08, writes (US-08)

Repository `/storage/repos/lighthouse-clients`, commit `f942462`. Harness as slice 01.

## Files

| File | Skipped | Active |
|---|---|---|
| `packages/cli/src/writeConfirmations.test.ts` | 15 | 9 |
| `packages/mcp-core/src/writeSummaries.test.ts` | 5 | 1 |
| `packages/cli/src/prettyForms.test.ts` rows for slice 08 | 11 | — |

## Scenarios

- All 11 write forms confirm in one line, e.g. `Created: Team Lightspeed [id: 9].`,
  `Refresh queued: Team [id: 3]. Lighthouse updates it in the background.`,
  `Created: recurring blackout rule [id: 5] — Every Friday — every 2 weeks — from 2026-10-09 — no end
  (Focus Friday).`, `Deleted: recurring blackout rule [id: 5].`
- Error/edge: an answer without a name → `Created: Team [id: 9].`; a rule without a description → no
  parentheses; renamed (`Squad`, `Programme`); terminology refused → seeded words.
- MCP: refresh and blackout tools keep today's block and add the confirmation as a second block.
- Guards: today's lines under `--json` / `--toon` for the five no-record writes (DSN-12); the written
  record unchanged for three create/update forms; a refused delete passes through.

## DTO against sketch

- The blackout schedule is the server's `summary` verbatim, `Every Friday — every 2 weeks — from
  2026-10-09 — no end`, not the sketch's `Every 2 weeks on Friday, from Fri 9 Oct 2026`. The scenarios
  follow the sketch's own rule (server words, not re-worded).
- `RecurringBlackoutRuleDto` already has a `summary` field, so the MCP blackout create/update cannot take
  ADR-224's object `summary`; decided: a second block. Raised upstream.
