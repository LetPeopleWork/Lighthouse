# Mutation testing — slice 09 "Refinement need for clients" (story 6147), lighthouse-clients

- Repo: `lighthouse-clients`. Production code frozen at `0086a16`. Killing tests committed as
  `9a53572 test(refinement): mutation kills for the refinement need (story 6147)` (not pushed).
- Tool: StrykerJS `@stryker-mutator/core@10.0.0`, run ephemerally (installed into a scratch
  directory, never in `package.json` or the lockfile). 10.0.0 was published 2026-08-14, older than the
  repo's 7-day `minimumReleaseAge`.
- Gate: ≥ 80 % kill rate. **Before 85.65 % → after 98.86 %.**

## Scores per file

| file | scope | mutants | before | after |
| --- | --- | ---: | ---: | ---: |
| `packages/client/src/refinementWording.ts` | whole file | 280 | 85.71 % (40 survived) | 98.21 % (5 survived, all equivalent) |
| `packages/cli/src/refinementOutput.ts` | whole file | 52 | 90.38 % (5) | 100.00 % |
| `packages/cli/src/index.ts` | changed lines only | 64 | 79.69 % (13) | 100.00 % |
| `packages/cli/src/output.ts` | the `renderPretty` seam, line 227 | 3 | 100.00 % | 100.00 % |
| `packages/client/src/index.ts` | `getTeamRefinement`, `getTerminology`, version-gate entry, re-export | 12 | 66.67 % (4) | 100.00 % |
| `packages/mcp-core/src/index.ts` | `lighthouse_team_refinement_get` (definition, handler, schema, dispatch) | 28 | 96.43 % (1) | 100.00 % |
| **all** | | **439** | **85.65 %** (376 killed / 63 survived) | **98.86 %** (434 killed / 5 survived) |

No timeouts, no errors, no uncovered mutants in either run. All six files appear in both per-file tables,
and the line-span entries produced mutants: 439 in total.

## Survivors and their classification

### (a) Real gaps, killed (58)

| mutant(s) | where | what survived | killing scenario |
| --- | --- | --- | --- |
| 2, 4, 5, 6, 7, 8 | `cli/index.ts:1181-1187` | the refinement group help text (each line, the blank line, the `\n` join) | CLI: "explains the refinement group in its help" (exact lines) |
| 20, 21, 22 | `cli/index.ts:2402` | `--team-id` regex anchors and `+` | CLI: refuses `3abc` and `abc3`; "reads a Team whose id has more than one digit" |
| 51, 54, 55 | `cli/index.ts:2452-2455` | unknown-subcommand branch and its group name | CLI: "refuses a refinement subcommand it does not know" |
| 58 | `cli/index.ts:2461` | the no-connection early return | CLI: "asks to connect first when no Lighthouse is connected" |
| 70, 71, 73 | `cli/refinementOutput.ts:15-21` | column widths (`max`→`min`, length→undefined) and `trimEnd` | CLI: "lines the Work Items up under their column headings" (column offsets, no trailing whitespace) |
| 113, 117 | `cli/refinementOutput.ts:73` | the blank line before the list, and none after a summary without a list | CLI: blank line asserted at `printed[2]`; nothing-in-refinement cases now assert exact stdout |
| 125, 126, 129, 130 | `client/index.ts:2150, 2158` | `{ method: "GET" }` on both reads | client: both reads assert `init.method === "GET"` |
| 132, 134 | `refinementWording.ts:23, 25` | the seeded `Work Item` and `Team` words | CLI: the fallback test asserts the `# Work Item` header; a nameless Team with unreadable terms is named "Team 3" |
| 159-164, 167, 170 | `refinementWording.ts:53-56` | the guards naming the Team | CLI: the Team read carries `null`, a bare string, or a non-text name → "Team 3" |
| 201, 202 | `refinementWording.ts:117` | singular Work Item only for exactly one | CLI: Below 1–3 → "Work Items"; Below 4 (one number) → "Work Items" |
| 237, 239, 241 | `refinementWording.ts:167-168` | `NoRefinementStates` on a configured Team | CLI: the no-states scenario runs for configured `false` and `true` |
| 259, 260, 270 | `refinementWording.ts:193, 197` | `CALENDAR_DAY` anchors and the `\|\|` before them | CLI: cycle start `2026-10-08-01`, cycle end `12026-10-08` → no verdict |
| 280, 281 | `refinementWording.ts:203-205` | the month/date conjunction | CLI: cycle end `2026-13-08` (Date rolls it into January 2027, keeping the date) → no verdict |
| 302-306, 308, 310 | `refinementWording.ts:220-222` | `verdict`, `low`, `high` null checks | CLI: a verdict without its verdict / low end / high end → heading and list only |
| 342, 345, 346, 347 | `refinementWording.ts:254` | the `NoCadence` disjunct and the whole cadence condition | CLI: NoCadence with a next Refinement date gives the cadence hint; incomplete facts give no hint (line 2 is the table header) |
| 360 | `refinementWording.ts:274` | a verdict that comes with an `unavailableReason` | CLI: "says why there is no number when a verdict comes with a reason there is none" |
| 377 | `refinementWording.ts:303` | `need === null` (a literal `null` line) | CLI: line 2 is the table header when there is no sentence |
| 396 | `refinementWording.ts:337` | `listed === 0` in the exported `placeEnoughForLine` | client: `placeEnoughForLine` with nothing listed → `null` (the CLI never calls it with an empty list) |
| 401 | `refinementWording.ts:340` | `<` → `<=` | CLI: 8 listed, 8 needed → the "enough for" line, not "All 8 … are needed" |
| 428 | `mcp-core/index.ts:1472` | the tool's registered input schema `{ id: int }` | MCP: "is registered with an MCP server as needing a whole-number Team id" |

### (b) Equivalent (5)

All five sit in `isCalendarDay` (`refinementWording.ts:196-207`). They were checked by brute force over
years {0, 1, 26, 99, 100, 1600, 1900, 2000, 2024, 2026, 2100, 9999} × months 00-99 × days 00-99
(the full range the `\d{2}` regex admits).

| mutant | mutation | why equivalent |
| --- | --- | --- |
| 271 | `value === null` → `false` | `RegExp.test(null)` tests the string `"null"`, which `CALENDAR_DAY` rejects, so null still returns `false`. |
| 285 | month check → `true` | A day that overflows its month always changes the date as well, and an overflowing month always changes the year, so the remaining two checks reject every input this one rejects. 0 differing inputs. |
| 288 | date check → `true` | Day 00 or a day past the month's end always moves the month too (at most ±3 months, never 12), so the month check rejects every input this one rejects. 0 differing inputs. |
| 282 | year `&&` month → `\|\|` | Differs only for years 0000-0099, where `Date.UTC` maps the year to 19xx. The server never sends such a date. Pinning a rejection there would pin a side effect of `Date`, not intended behaviour. |
| 283 | year check → `true` | Same as 282: it differs from the original only for years 0000-0099. |

## Observation (not a gap the gate measures, production code untouched)

`describeNeed` gives no verdict when `unavailableReason` is set, even if a full verdict comes with it.
`placeEnoughForLine` and `countNumberedRows` do not check `unavailableReason`. So in that
contradictory payload the CLI says "Not enough data yet …", yet it still numbers the rows and draws the
"enough for" line. The server is not expected to send a verdict together with a reason. The new
scenario pins only the sentence. Raise it if the web tab handles that case differently.

## Command

Run from the repository root of `lighthouse-clients`, with Stryker installed in the scratch directory
`$S` (`npm install --no-save --legacy-peer-deps @stryker-mutator/core@10.0.0
@stryker-mutator/vitest-runner@10.0.0` into a throwaway `package.json`, plus a symlink
`$S/node_modules/vitest` → the repo's own `vitest@5.0.1`):

```
TZ=America/Adak $S/node_modules/.bin/stryker run $S/stryker.config.json
```

The sandbox and the reports live under `$S`. The repo's `git status` was clean after every run.

## Config

```json
{
  "testRunner": "command",
  "commandRunner": { "command": "node_modules/.bin/vitest run --bail=1 --reporter=dot" },
  "inPlace": false,
  "tsconfigFile": "no-tsconfig-rewrite.json",
  "ignorePatterns": ["dist", "agent-output", "docs", "skill", ".changeset"],
  "disableTypeChecks": false,
  "coverageAnalysis": "off",
  "concurrency": 4,
  "timeoutMS": 20000,
  "tempDirName": "<scratch>/mut6147/.stryker-tmp",
  "cleanTempDir": true,
  "reporters": ["clear-text", "json", "html", "progress"],
  "jsonReporter": { "fileName": "<scratch>/mut6147/reports/mutation-report.json" },
  "htmlReporter": { "fileName": "<scratch>/mut6147/reports/mutation-report.html" },
  "mutate": [
    "packages/client/src/refinementWording.ts",
    "packages/cli/src/refinementOutput.ts",
    "packages/cli/src/index.ts:206-210",
    "packages/cli/src/index.ts:1179-1188",
    "packages/cli/src/index.ts:1216-1216",
    "packages/cli/src/index.ts:2395-2471",
    "packages/cli/src/index.ts:2583-2586",
    "packages/cli/src/output.ts:227-227",
    "packages/client/src/index.ts:1953-1953",
    "packages/client/src/index.ts:2141-2159",
    "packages/client/src/index.ts:2878-2879",
    "packages/mcp-core/src/index.ts:759-764",
    "packages/mcp-core/src/index.ts:1289-1315",
    "packages/mcp-core/src/index.ts:1472-1472",
    "packages/mcp-core/src/index.ts:1759-1767"
  ],
  "thresholds": { "high": 90, "low": 80, "break": null }
}
```

The line spans come from `git diff -U0 3bbf78b..HEAD`. Spans that only change imports and types are
left out, because they carry no mutants.

## Run notes (traps hit on the way)

1. **The vitest runner gives meaningless scores against vitest 5.** With `@stryker-mutator/vitest-runner`
   10.0.0, every run scored **6.83 %**. Only the 30 static mutants were killed (each gets a fresh vitest
   instance), and the runner averaged 8-28 tests per mutant. Turning off `vitest.related` or bail
   changed nothing. This is the same vitest-5 fault the Lighthouse frontend memory records. The
   **`command` runner** (a full `vitest run` per mutant, activated via `__STRYKER_ACTIVE_MUTANT__`)
   gives sound results: 7 minutes for 439 mutants at concurrency 4.
2. **The time-zone scenario needs `TZ` set from outside.**
   `refinementWording.timezone.test.ts` sets `process.env.TZ` at runtime. Inside Stryker's worker
   threads that change does not reach ICU, so the dry run failed. Running Stryker under
   `TZ=America/Adak` keeps it honest, and the whole suite passes in that zone (370/370).
3. **`inPlace` with a temp dir on another filesystem leaves the repo instrumented.** Stryker restores
   by `rename`, which fails with `EXDEV` between `/tmp` and `/storage`. The first attempt left six
   instrumented files behind; they were restored with `git checkout -- packages/` (the slice was
   committed). Sandbox mode is safe.
4. **Sandbox mode needs the tsconfig rewrite switched off.** TypeScript 7 has no
   `parseConfigFileTextToJson`, so Stryker's `TSConfigPreprocessor` would crash.
   `"tsconfigFile": "no-tsconfig-rewrite.json"` (a file that does not exist) skips it.
