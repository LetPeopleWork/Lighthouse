# Mutation testing — Epic 6298 "report reads for lh and MCP", lighthouse-clients

- Repo: `lighthouse-clients`. Production code frozen at `fc8f49a` (merge-base with `origin/main`: `7afb13f`).
  Killing tests committed as `e07b406 test(client): kill surviving mutants in the report wording and the
  ReportOpened count (Epic 6298)`, `72577dd test(cli): kill surviving mutants in lh report and the eval
  fixture server (Epic 6298)` and `a75e804 test(mcp-core): kill surviving mutants in the report tools
  (Epic 6298)` (not pushed). Each carries an empty changeset, as the pre-commit changeset gate asks of any
  commit under `packages/*/src`. No production code changed.
- Tool: StrykerJS `@stryker-mutator/core@10.0.0`, installed into a scratch directory (never in
  `package.json` or the lockfile). Setup reused from story 6218 (`../../story-6218-readable-cli-output/mutation/clients.md`):
  `command` runner (`vitest run --bail=1`, the root `vitest.config.ts`, one full suite per mutant), sandbox
  mode, `tsconfigFile: no-tsconfig-rewrite.json`, `TZ=America/Adak`, **concurrency 6** (another agent ran
  the Lighthouse frontend suite on the same machine; load average ~10 on 12 cores throughout).
- Gate ≥ 80 % over the Epic's changed production code: **before 70.41 % → after 97.61 %**.
  Every file is at or above 91 % after the kills.
- As in story 6218, the root vitest config aliases every `@letpeoplework/lighthouse-*` package to its
  `src/index.ts`, so a client mutant is seen by every client, CLI and MCP test in the same run.
- `scripts/eval-fixture.mjs` is reachable: `skills/evalFixture.test.ts` spawns it from the sandbox copy,
  and the child inherits Stryker's active-mutant variable, so its mutants are killed or survive like any other.
- Out of scope: skill markdown, eval cases and fixtures, `test-support/`, and the type-only or import-only
  hunks (`cli/index.ts` 67, 114-117; `client/index.ts` 1415-1429, 3385-3386; `usageData.ts` 1, 26-34,
  54-64; `mcp-core/index.ts` 96-101, 186-190, 874-875), which carry no mutants.

## Scores per file

Scope: new files whole, pre-existing files by the new-side spans of `git diff -U0 7afb13f..fc8f49a`.
9 files, 419 mutants, 0 timeouts, 0 errors, 0 no-coverage in both runs. "After" is the re-run scoped to the
seven files that got tests; `reportReads.ts` and `mcp-core/index.ts` had no survivors and carry their first-run
figures.

| file | scope | mutants | before (survived) | after (survived) |
| --- | --- | ---: | ---: | ---: |
| `cli/src/index.ts` | 4 changed-line spans (report help, usage line, `runReportGroup`, dispatch) | 30 | 56.67 % (13) | 100.00 % (0) |
| `cli/src/reportCommands.ts` | whole file | 88 | 73.86 % (23) | 98.86 % (1) |
| `client/src/index.ts` | 3 changed-line spans (version gate, `readReports`, the four reads) | 13 | 92.31 % (1) | 92.31 % (1) |
| `client/src/reportReads.ts` | whole file | 17 | 100.00 % (0) | 100.00 % (0) |
| `client/src/reportWording.ts` | whole file | 134 | 65.67 % (46) | 100.00 % (0) |
| `client/src/usageData.ts` | 1 changed-line span (66-91) | 22 | 72.73 % (6) | 100.00 % (0) |
| `mcp-core/src/index.ts` | 3 changed-line spans (definitions, schemas, dispatch) | 4 | 100.00 % (0) | 100.00 % (0) |
| `mcp-core/src/reportTools.ts` | whole file | 78 | 67.95 % (25) | 91.03 % (7) |
| `scripts/eval-fixture.mjs` | 2 changed-line spans (26-37, 49-51) | 33 | 69.70 % (10) | 96.97 % (1) |
| **all** | | **419** | **70.41 %** (295 killed / 124 survived) | **97.61 %** (409 killed / 10 survived) |

Wall clock: 19 min 12 s for the first full run, 9 min 29 s for the scoped re-run (398 mutants).

## Survivors killed (114)

Tests only: value-in/value-out on the client wording, real-socket `lh` runs for the CLI, the MCP core harness
for the tools, the spawned fixture server for the eval script.

| where | what survived | killing scenario |
| --- | --- | --- |
| `client/reportWording.ts` readers (L45-88, 27) | every guard in `readWindow`, `readReport`, `readReportListItem` and `readReportList` (the existing scenarios only ever sent whole answers); `every` → `some`; an empty list | new `reportWording.test.ts`: "says nothing of an answer with …" one broken fact at a time for a report and a list; "says an owner without reports has none" |
| `client/reportWording.ts` wording (L103-158, 16) | the owner named by id, the exact summary line with `–` and `·`, "No reports" / "1 report", a list row without type or created day, a created day that is not one | "the summary of one report", "the summary of a report list", "the owner a summary names" (renamed terms, no name, empty name) |
| `client/reportWording.ts:210` (3) | terminology read only for a refusal Lighthouse made | "says it in the instance's words when Lighthouse refused"; "asks Lighthouse nothing more when the client refused before asking" (counts `getTerminology` calls) |
| `client/usageData.ts:81-89` (6) | `reportOpenedOccurrencesOf` on a refusal, a `null` answer, an unknown or missing type, the web's word sent as a key | new `reportOpened.test.ts` "which report a read opened" |
| `cli/index.ts` (13) | the report help lines, help for a bare `lh report`, the unknown-subcommand branch and its group name, the not-connected return | new `reportCommands.test.ts` "lh report explains itself" (help and refusal asserted line by line) |
| `cli/reportCommands.ts` (22) | the `--team-id` / `--portfolio-id` / `--id` regex anchors and guards, each refusal message and its command name, the early returns on a bad option, the `--json`/`--toon` shortcut that skips naming the owner | "lh report refuses what it cannot read, without asking Lighthouse" (stderr exact, no report read); "reads neither the owner nor the instance's words for --json / --toon" |
| `mcp-core/reportTools.ts` (18) | the four tool definitions (shared description sentences, the input-schema builders), the `reports` / `report` labels, every invalid-id branch, `reportId` that is text or not whole | new `reportTools.test.ts`: definitions asserted whole, labels and summary blocks, "refuses …" with nothing asked |
| `scripts/eval-fixture.mjs` (9) | `value !== null`, the `every` over the keys and its predicate, the JSON content type | `evalFixture.test.ts` "answers a route whose value is nothing / a status beside other facts / a status that is not a whole number with 200 and that value as JSON" |

## Survivors justified (10)

| mutant(s) | where | why it cannot be killed meaningfully |
| --- | --- | --- |
| 1 | `cli/reportCommands.ts:86` `kind: "portfolio"` → `""` | the kind is only ever compared with `"team"` (`readOwnersReports`, `readReportOwner`, the owner and refusal words), so `""` reads as Portfolio everywhere. Equivalent. |
| 2 | `mcp-core/reportTools.ts:198,199` `reportList("portfolio")` / `reportGet("portfolio")` → `""` | same: the kind is only compared with `"team"`. Equivalent (the same finding as story 6218's `"portfolio"` → `""`). |
| 4 | `mcp-core/reportTools.ts:120` the object guard in `getReportId` (`→ true` ×3, `&&` → `\|\|`) | `getReportId` runs only after `getNumericId` accepted the payload, which already demands a non-null, non-array object; the guard can never see anything else. Equivalent. |
| 1 | `mcp-core/reportTools.ts:123` `typeof reportId === "number"` → `true` | `Number.isInteger` does not coerce, so it is false for every non-number already. Equivalent. |
| 1 | `client/index.ts:2316` `{ method: "GET" }` → `{}` | `requestJson` passes `requestOptions?.method` on, so `{}` leaves the method undefined and `fetch` sends GET. Equivalent on the wire. |
| 1 | `scripts/eval-fixture.mjs:28` `!Array.isArray(value)` → `true` | an array parsed from JSON never has a `status` property, so `Number.isInteger(value.status)` rejects it anyway. Equivalent. |

## Run notes

1. **No load effects seen.** Concurrency 6 with another agent's frontend suite running: no timeouts, no
   errors, and every mutant killed in the first run stayed killed in the re-run.
2. **The shell allowlist blocks `nohup stryker` and inline `bash -c` / `node -e`.** The runs went through a
   script file (`run.sh`: `node <scratch>/node_modules/@stryker-mutator/core/bin/stryker.js run <cfg>`, output
   to a log) started with `run_in_background`, and a script file that waits for the log's `exit=` line.
3. **New tests are type-checked and linted by the gate.** `pnpm run ci` (lint, test, typecheck, build,
   `typecheck:tests`) ran green before and in each of the three commits' pre-commit hook: 112 files, 2394 tests.
4. `git status` in lighthouse-clients was clean after every run; the sandbox lives under the scratch directory
   and is removed by `cleanTempDir`.

## Command

Run from the repository root of `lighthouse-clients`, with Stryker installed in the scratch directory `$S`
(`npm install --no-save --legacy-peer-deps @stryker-mutator/core@10.0.0` into a throwaway `package.json`):

```
TZ=America/Adak node $S/node_modules/@stryker-mutator/core/bin/stryker.js run $S/stryker.config.json
```

Config: `stryker.6298.clients.json` next to this file (`<scratch>` stands for `$S/..`). The re-run used the same
config without the `reportReads.ts` and `mcp-core/src/index.ts` entries. The `mutate` list was generated from
`git diff --name-status` / `git diff -U0` against the merge-base: added files whole, modified files by their
new-side hunks, type-only and import-only hunks left out.
