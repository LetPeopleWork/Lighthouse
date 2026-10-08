# Mutation testing — story 6218 "every lh --pretty command reads like the web; MCP summaries beside unchanged facts", lighthouse-clients

- Repo: `lighthouse-clients`. Production code frozen at `a85e6e4` (merge-base with `origin/main`: `2088950`).
  Killing tests committed as `3aed5c0 test(cli): kill surviving mutants in the readable --pretty output and
  MCP summaries (story 6218)` and `63c0c34 test(client): kill surviving mutants in the metrics and Team
  wording (story 6218)` (not pushed). No production code changed.
- Tool: StrykerJS `@stryker-mutator/core@10.0.0`, installed into a scratch directory (never in
  `package.json` or the lockfile). Setup reused from slice 09 (`../../epic-5510-5881-refinement/mutation/clients-slice-09.md`):
  `command` runner (`vitest run --bail=1`, one full suite per mutant), sandbox mode,
  `tsconfigFile: no-tsconfig-rewrite.json`, `TZ=America/Adak`, concurrency 11.
- Gate ≥ 80 % over the story's changed production code: **before 87.03 % → after 97.64 %**.
  Every story file is at or above 85 % after the kills.
- `mcp-http`: no production line changed by the story (the diff touches no `packages/mcp-http/src/*.ts`
  outside tests), so it is not in scope. `mcp-stdio`: same.

## The client-dist question

The CLI and MCP tests do **not** see the client's built `dist` under vitest: the root `vitest.config.ts`
aliases every `@letpeoplework/lighthouse-*` package to its `src/index.ts`. A mutant in
`packages/client/src` is therefore seen by every client, CLI and MCP test in the same run, with no
per-mutant rebuild. The only tests that read a `dist` are the `mcp-http`/`mcp-stdio` bin launch guards,
which `skipIf` the built `bin.js` is missing — and the sandbox ignores `dist`, so they skip there. The
client scores below are therefore the whole suite's kill rate, not only the client package's own tests.

## Scores per file

Scope: new files whole, pre-existing files by the new-side spans of `git diff -U0 2088950..a85e6e4`
(type-only and import-only hunks carry no mutants). 28 files, 3476 mutants, 0 timeouts, 0 errors,
0 no-coverage in every run. "After" is the full re-run with the first test commit, with the files that
got further tests re-run scoped (`metricsOutput`, `writeOutput`, `housekeepingWording`, `mcp-core/index`;
then `metricsWording`, `ownerWording`).

| file | scope | mutants | before (survived) | after (survived) |
| --- | --- | ---: | ---: | ---: |
| `cli/src/commandResult.ts` | 2 changed-line spans | 8 | 100.00 % (0) | 100.00 % (0) |
| `cli/src/deliveryOutput.ts` | whole file | 46 | 80.43 % (9) | 100.00 % (0) |
| `cli/src/featureOutput.ts` | whole file | 22 | 81.82 % (4) | 100.00 % (0) |
| `cli/src/forecastOutput.ts` | whole file | 75 | 84.00 % (12) | 98.67 % (1) |
| `cli/src/housekeepingOutput.ts` | whole file | 43 | 95.35 % (2) | 100.00 % (0) |
| `cli/src/index.ts` | 41 changed-line spans | 151 | 86.09 % (21) | 94.70 % (8) |
| `cli/src/metricsOutput.ts` | whole file | 257 | 73.54 % (68) | 98.05 % (5) |
| `cli/src/output.ts` | 3 changed-line spans | 7 | 85.71 % (1) | 85.71 % (1) |
| `cli/src/ownerOutput.ts` | whole file | 44 | 79.55 % (9) | 97.73 % (1) |
| `cli/src/table.ts` | whole file | 8 | 100.00 % (0) | 100.00 % (0) |
| `cli/src/writeOutput.ts` | whole file | 22 | 95.45 % (1) | 100.00 % (0) |
| `client/src/answerWording.ts` | whole file | 41 | 70.73 % (12) | 95.12 % (2) |
| `client/src/calendarDates.ts` | whole file | 61 | 90.16 % (6) | 90.16 % (6) |
| `client/src/deliveryWording.ts` | whole file | 275 | 89.45 % (29) | 99.27 % (2) |
| `client/src/featureWording.ts` | whole file | 170 | 96.47 % (6) | 99.41 % (1) |
| `client/src/forecastDisplayRules.ts` | whole file | 68 | 100.00 % (0) | 100.00 % (0) |
| `client/src/forecastWording.ts` | whole file | 179 | 93.85 % (11) | 99.44 % (1) |
| `client/src/housekeepingWording.ts` | whole file | 268 | 96.27 % (10) | 99.25 % (2) |
| `client/src/index.ts` | 4 changed-line spans | 4 | 100.00 % (0) | 100.00 % (0) |
| `client/src/metricsWording.ts` | whole file | 942 | 87.05 % (122) | 98.62 % (13) |
| `client/src/ownerWording.ts` | whole file | 191 | 98.43 % (3) | 100.00 % (0) |
| `client/src/refinementWording.ts` | 6 changed-line spans | 2 | 100.00 % (0) | 100.00 % (0) |
| `client/src/terminology.ts` | whole file | 49 | 100.00 % (0) | 100.00 % (0) |
| `client/src/wireFacts.ts` | whole file | 53 | 96.23 % (2) | 96.23 % (2) |
| `client/src/writeWording.ts` | whole file | 48 | 97.92 % (1) | 97.92 % (1) |
| `mcp-core/src/index.ts` | 87 changed-line spans | 404 | 70.79 % (118) | 91.09 % (36) |
| `mcp-core/src/toolResult.ts` | 1 changed-line span | 38 | 89.47 % (4) | 100.00 % (0) |
| **all** | | **3476** | **87.03 %** (3025 killed / 451 survived) | **97.64 %** (3394 killed / 82 survived) |

Wall clock: 53 min for the first full run, 41 min for the re-run, 14 min and 9 min for the scoped runs.

## Survivors killed (369)

Tests only, value-in/value-out on the client wording modules where the mutant lives there, CLI or MCP
tests where it is in wiring.

| where | what survived | killing scenario |
| --- | --- | --- |
| `client/metricsWording.ts` readers (L47-576, ~95) | every `isRecord && isDay && isNumber …` check in the wire readers, the `!isRecord` guards (a `null` section threw instead of reading as unknown), the refusal shape, the regex anchors of a run-chart offset, the WIP limit of exactly 1, a definition name that is not text | `metricsWording.test.ts` "what each metrics section must look like to be read": one fact broken at a time per section, `null` for every section, contributors and candidates field by field |
| `client/metricsWording.ts` wording (L645-1102) | the percentile and Work Item Age labels, `daysInRange` on midnight timestamps, blocked count with only some items flagged, the empty detail beside a Total Work Item Age with no day, oldest-first with items without an age, the day's oldest Work Item when it is not last | "the metrics wording at its edges" |
| `client/deliveryWording.ts` (27) | every field check of a recorded day, a Feature and a chance; a non-numeric likelihood; the Feature count not sent; the cap on a finished day's or Feature's likelihood | "what a Delivery's facts must look like to be stated" |
| `client/answerWording.ts` (10) | the name guards (a `null` or bare-text read threw), the empty name, the Feature read by its id | new `answerWording.test.ts` |
| `client/forecastWording.ts`, `featureWording.ts`, `housekeepingWording.ts`, `ownerWording.ts` (~25) | chance readers on a `null` entry, `null` answers, the How Many title's two conditions; a non-numeric Team share; each weekday name, a non-weekday; a connection id; options labelled by the connection's own method, a `null` declared option; WIP limit 0 / text, Throughput dates with either end missing, a non-boolean fixed flag | additions to each module's test file |
| `cli/*Output.ts` null guards (~20) | each renderer's "null for an answer I do not know" contract (removing the guard throws instead) | `readableOutput.edges.test.ts` "a pretty view handed an answer it does not know" |
| `cli/*Output.ts` blank lines (~15) | blank lines between a view's parts (`shownLines` drops empty lines, so nothing pinned them): the latest Delivery day, the chance table left out for `[]` and `null`, forecast sections, the Portfolio list pointer | "the blank lines between a view's parts" (lines printed with blanks kept) |
| `cli/metricsOutput.ts` (63) | each section's part label when unknown or refused, sections absent, no percentile rows, the over-time heading with no over-time section, the exact headline layout, day views one blank line apart, every refused part leaving a day view to the generic one | "the metrics views at their edges" |
| `cli/index.ts` (13) | the `--target-date` read, when Team settings are read for a cycle time definition (no definition, other metrics, no `--metrics`, Portfolio), a Team settings or Team read that throws, the delivery list heading when the Portfolio throws or is unreadable | "lh reads it needs only for its words" |
| `mcp-core/index.ts` (82) | the 24 tool descriptions; every per-metric tool's facts label and summary heading (Team name, the given range, Portfolio labels, Portfolio Work Item Age); the Portfolio fallback name; Team settings not read without a definition; percentile history of another family; process limits for `Throughput` / other families; the refusal text of `team_list`, `team_get`, `feature_get`, `feature_workitems`, `delivery_list`, `blackout_create`, `blackout_delete`, `worktracking_list`, `forecast_backtest`; no empty line for a forecast without a target date | `metricSummaries.everyTool.test.ts`, `summaryRefusals.test.ts` |
| `mcp-core/toolResult.ts` (4) | `withSummary` on a list, `null`, a scalar, an object with its own `summary` | new `toolResult.test.ts` |

## Survivors justified (82)

| mutant(s) | where | why it cannot be killed meaningfully |
| --- | --- | --- |
| 4 | `cli/index.ts:1844,1868,1887,1905` `"portfolio"` → `""` | the confirmation picks its word with `kind === "team" ? team : portfolio`; `""` reads as Portfolio. Equivalent. |
| 2 | `cli/index.ts:2065,2151` catch → `{}` | `{}.ok` is falsy, the same branch as `{ ok: false }`. Equivalent. |
| 1 | `cli/index.ts:2392` `terms === null` → `false` | a renderer with no terms throws in the heading, and the generic-view fallback prints what no renderer prints. Equivalent. |
| 1 | `cli/index.ts:2420` catch → `undefined` | `portfolio?.ok` treats `undefined` as it treats `null`. Equivalent. |
| 4 | `cli/metricsOutput.ts:280` `answered()` | it returns the answer itself, so a `null` answer comes back `null` either way; `undefined` never reaches it (the drill-down is checked first); a refusal is still caught by `isMetricRefusal`. Equivalent. |
| 1 | `cli/metricsOutput.ts:475` `entry === undefined` → `false` | `entry?.render` gives `null` for an unknown name, caught by `view === null`. Equivalent. |
| 1 | `cli/output.ts:223` `renderPretty?.()` → `renderPretty()` | a missing renderer throws, and the catch prints the same generic view. Equivalent. |
| 1 | `cli/ownerOutput.ts:76` `?.length` → `.length` | the table is null exactly when the list is unreadable, so the second operand never sees `null`. Equivalent. |
| 1 | `cli/forecastOutput.ts:96` header default `""` | `toTableLines` always returns the header line. Unreachable. |
| 2 | `client/answerWording.ts:44` `UNREAD` → `{}` / `ok: true` | both still route to the fallback name (`ok` undefined is falsy; `ok: true` with a `null` value has no name). Equivalent. |
| 6 | `client/calendarDates.ts:23,29-31,46` | `null` fails the day regex anyway (23, 46). Brute force over years {0, 1, 26, 99, 100, 1600, 1900, 2000, 2024, 2026, 2100, 9999} × months 00-99 × days 00-99: the month → `true` and day → `true` mutants differ on no input; year → `true` and year `\|\|` month differ only for years 0000-0099, which `Date.UTC` maps to 19xx and Lighthouse never sends. Same finding as slice 09's `isCalendarDay`. |
| 2 | `client/wireFacts.ts:12` | `Number.isFinite` does not coerce, so it is false for every non-number already. Equivalent. |
| 1 | `client/writeWording.ts:25` | `{ id, name: undefined }` vs `{ id }`: every consumer reads `.name`. Equivalent. |
| 1 | `client/deliveryWording.ts:41` `flagOf` → identity | the flags are only compared with `=== true` / `=== false`, which a non-boolean never matches. Equivalent. |
| 1 | `client/deliveryWording.ts:218` `>` → `>=` | differs only for two recorded days on the same date; Lighthouse records one point per day. |
| 1 | `client/featureWording.ts:41` `{}` | same as both fields `undefined`. Equivalent. |
| 1 | `client/forecastWording.ts:150` `"fixed2"` → `""` | `formatLikelihood` treats anything but `"round"` as two decimals. Equivalent. |
| 2 | `client/housekeepingWording.ts:148,156` fallback `[]` → `["Stryker was here"]` | a string entry is never a record and never has the option's key. Equivalent. |
| 2 | `client/metricsWording.ts:160` the run chart sort | integer-like keys enumerate in ascending order already, and the offset regex rejects every other key except zero-padded ones (`"01"`), which Lighthouse never sends. |
| 1 | `client/metricsWording.ts:361` `{ score: undefined }` → `{}` | same reading. Equivalent. |
| 1 | `client/metricsWording.ts:570` fallback list | a string is never a record. Equivalent. |
| 2 | `client/metricsWording.ts:762,769` `>=` / `<` at the bounds | differ only for two entries on the same day. |
| 1 | `client/metricsWording.ts:1102` `>` → `>=` | which of two equally old Work Items is named oldest; the web specifies no tie-break, so pinning one would pin an accident. |
| 6 | `client/metricsWording.ts:876,1159` | `first` and `last` come from the same list, so they are `undefined` together; any one of the two checks decides alone. Equivalent. |
| 19 | `mcp-core/index.ts:1427,1559,1574,1585,1604,1619,1637 (×3),1654,1671,1690,1707,1722,1727,1742,1753,1766` and the `readForSummary` catch block (1393) | each guard only saves the describer from a `null`; without it the describer throws (checked: every describer involved throws on `null`) and `summaryOrNull` leaves the summary out, which is what the guard does. The catch block returns `undefined` either way. Equivalent. |
| 1 | `mcp-core/index.ts:1608` `[]` → `["Stryker was here"]` | the closed Work Items only feed the day table, and the summary carries the sentence and notes only. Equivalent. |
| 14 | `mcp-core/index.ts:2216,2319,2349,2350,2389,2427,2508,2510,2536,2571,2572,2620,2628,2642` `"portfolio"` → `""` | the scope is only ever compared with `"team"`, so `""` reads as Portfolio (owner kind, wording read and counted word alike). Equivalent. |
| 2 | `mcp-core/index.ts:2446,2592` `"team"` → `""` | these summaries carry no counted word: Time in State is told without its Work Item count, and the Work Item Age summary is the percentile sentence. Equivalent in what reaches the assistant. |

## Run notes

1. **Flaky kills under load.** Five mutations killed in the first full run survived the re-runs
   (e.g. `mcp-core/index.ts:1753`, an equivalent `null` guard), and the survivor set of the
   `mcp-core/index.ts` safety-net guards shifted by one or two between runs. At concurrency 11 an
   unrelated test occasionally fails under load and `--bail` counts it as a kill. All of them are in the
   justified list above; the "before" score may be inflated by about 0.1 point.
2. **The first re-run was killed mid-way** (no Stryker error in its log; the process ended at 47 %).
   The re-run was started again from a clean sandbox and completed.
3. **New tests are type-checked by hand.** `tsc -b` leaves test files out, so the touched tests were
   checked with a temporary `tsconfig` that includes them (then deleted): clean.
4. `git status` in lighthouse-clients was clean after every run; the sandbox lives under the scratch
   directory and is removed by `cleanTempDir`. `pnpm run ci` exit 0 after the last commit: 65 files,
   1452 tests (1240 before).

## Command

Run from the repository root of `lighthouse-clients`, with Stryker installed in the scratch directory
`$S` (`npm install --no-save --legacy-peer-deps @stryker-mutator/core@10.0.0` into a throwaway
`package.json`):

```
TZ=America/Adak $S/node_modules/.bin/stryker run $S/stryker.config.json
```

Config: `stryker.6218.clients.json` next to this file (`<scratch>` stands for `$S/..`). The `mutate` list
was generated from `git diff --name-status` / `git diff -U0` against the merge-base: added files whole,
modified files by their new-side hunks.
