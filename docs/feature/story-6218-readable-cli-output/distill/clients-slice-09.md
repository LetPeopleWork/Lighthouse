# DISTILL — slice 09, housekeeping and KPI-1 (US-09)

Repository `/storage/repos/lighthouse-clients`, commit `f942462`. Harness as slice 01.

## Files

| File | Skipped | Active |
|---|---|---|
| `packages/cli/src/housekeepingView.test.ts` | 13 | 9 |
| `packages/mcp-core/src/housekeepingSummaries.test.ts` | 8 | 2 |
| `packages/cli/src/prettyForms.test.ts` rows for slice 09 | 6 | 2 (completeness guards) |

## Scenarios

- `blackout list`: `Recurring blackout rules`, `Schedule · Description`, `[id: 5] <server summary>`;
  none → `No recurring blackout rules.`; a rule without a description leaves the cell empty.
- `worktracking list`: `Work Tracking Systems`, `Name · Type`, `Letpeoplework Jira [id: 1] Jira`.
- `worktracking get`: name and id, `Type: Jira`, `Option · Value` with the editor's labels; a secret
  reads `(secret, not shown)` **even when the server wrongly sends its value** (AC-09.2, the risk
  carrier; the list is checked too); a missing label falls back to the option key.
- `version get` → `Lighthouse v26.10.3.6`; `health check` → `Lighthouse at https://… is reachable.` and
  `The standalone Lighthouse is reachable.`
- Error/edge: renamed (`Trackers`); terminology refused; a connection without options → generic view.
- MCP: `summary: Lighthouse is reachable.` (M5); `summary: Lighthouse v26.10.3.6`;
  `3 Work Tracking Systems` / `2 recurring blackout rules` and their singulars; renamed; the connection's
  `summary` field names it and its type and never carries an option value.
- Guards: an unreachable Lighthouse reports `unreachable: connection refused`, exit 1 (AC-09.3);
  `success` under `--json` / `--toon`; `--json` / `--toon` for four forms (AC-09.4); the facts formats
  hand a connection over exactly as sent; refusals pass through.

## KPI-1 (`prettyForms.test.ts`)

42 forms change (41 rows plus the standalone health check), 10 stay unchanged on purpose, each with its
reason (refinement ×4, connection ×3, config ×2, help). Two active guards fail when a subcommand the help
offers, or a metric name `lh` accepts, is on neither list. The 42 skipped rows (one per form, tagged with
its slice) check the pretty output differs from the generic view of the same facts; un-skip each slice's
rows with that slice. KPI-1 reaches 0 when all 42 pass.

## Decided in DISTILL

- Option labels come from the connection's `availableAuthenticationMethods` (`Jira URL`, `Username
  (Email)`, `API Token`), the editor's own labels, rather than the sketch's `Url` / `Username` / `Api
  Token`; the key is the fallback.
- `Lighthouse ` + the version as sent (no extra `v`).
- Count sentence `2 recurring blackout rules` / `1 recurring blackout rule`.
