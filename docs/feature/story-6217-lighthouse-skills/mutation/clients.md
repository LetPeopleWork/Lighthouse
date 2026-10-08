# Lighthouse skills: clients mutation results

- Run date: 2026-10-08.
- Tool: StrykerJS 10 with the command runner (`vitest run --bail=1`), sandboxed, `TZ=America/Adak`. It mutates
  only the lines in `packages/*/src` that the feature changed since `0bf7e0e`. The config is in this folder as
  `stryker.6217.clients.json`; its ranges were regenerated after the commits below. The baseline config differed
  only in `client/src/index.ts`, where those lines sat 10 lines further on.
- Baseline at `ffcbcfe`: **88.86 %** (588 killed + 2 timed out of 664; 74 survived).
- Final at `693855e`: **96.50 %** (633 killed + 1 timed out of 657; 23 survived). One rerun was made with the
  same config. It had finished before the maintainer decided against further reruns.
- Commits in lighthouse-clients: `f171de3` (refactor), `8dee558`, `2220f97` and `693855e` (tests).

| File | Before | After |
|---|---|---|
| cli `index.ts` | 94.03 % | 97.01 % |
| cli `metricsOutput.ts` | 80.00 % | 85.26 % |
| client `index.ts` | 91.30 % | 93.75 % |
| client `metricsWording.ts` | 94.72 % | 100 % |
| client `processBehaviorChartTypes.ts` | 44.44 % | 100 % |
| mcp-core `index.ts` | 83.23 % | 96.41 % |

## What changed

- **One production change, a refactor rather than a fix.** `isProcessBehaviorMetricType` checked a table that
  mapped every chart type to `true`, and only asked whether a key was there. No test could tell those values from
  `false`, which accounts for 5 of the file's 9 mutants. The table also listed, a second time, the chart types the
  route table in `client/src/index.ts` already lists. The route table now lives in `processBehaviorChartTypes.ts`
  and the check reads its keys. Behaviour is unchanged.
- **Tests added**, each pinning what a user sees:
  - client: an SLE Risk entry whose miss count is text or missing is not read; an SLE of one day at 1 % is read;
    a Team that could not be read gets no sentence for an empty SLE Risk, and a count without the SLE's numbers
    otherwise; every signal, Moderate Shift included, is named; a chart day that is not an object, lacks its date
    or its signals, or carries an unknown signal next to a known one makes the chart unreadable.
  - cli: a refused chart prints its title and the refusal; a chart in an unknown shape says it is shown only with
    `--json`; SLE Risk for an unreadable Team with nothing at risk prints exactly the heading and the title; the
    Work Items in progress are not read for a Team's other metrics, nor read as a Team for a Portfolio.
  - mcp-core: the WIP, SLE Risk and both chart tools refuse a missing or non-whole id without asking Lighthouse;
    the Portfolio chart tool refuses a question with no chart named; the registered schemas pass the question on
    whole, and the Team chart's rejects Feature Size; the listed chart schemas require `id` and `metricType`, offer
    each tool's own charts, and allow nothing else; SLE Risk for an unreadable Team names it by its id.

## Survivors left, and why

| File | Line | Mutant | Class | Why |
|---|---|---|---|---|
| cli `index.ts` | 2239 | `.catch(...)` handler → `() => undefined` | Defensive | The Lighthouse client turns every failed request into a refusal result and never rejects, so the catch is never reached. |
| cli `index.ts` | 2240 | `?.items` → `.items` | Not classified | Killed in the baseline run and survived the rerun. A Work Items in progress answer in an unknown shape should reach this. Not investigated. |
| cli `metricsOutput.ts` | 347 | WIP sentence `join("\n")` → `join("")` | Not classified | Killed in the baseline run and survived the rerun. Not investigated. |
| cli `metricsOutput.ts` | 417 | `entries === null` → `false` | Equivalent | An unreadable SLE Risk then throws inside the renderer. `renderedOrGeneric` catches that and prints the generic view, which is what `null` prints. |
| cli `metricsOutput.ts` | 438 (4) | `isRecord` conditions | Defensive | The CLI always builds `processBehaviorChart` as `{startDate, endDate, charts}`, and `charts` is a record. Any other shape throws, and the throw falls back to the generic view, as `null` does. |
| cli `metricsOutput.ts` | 473 (3) | section/charts guard negated or emptied | Defensive | Same reason as the line above. |
| cli `metricsOutput.ts` | 477 (3) | `every(isKnownChart)` → `false`, `some`, or its block emptied | Defensive | The chart keys are the CLI's own chart-type lists, so an unknown key cannot arrive. |
| cli `metricsOutput.ts` | 483 | `rows.length === 0` → `false` | Defensive | The CLI always asks for 5 charts (Team) or 6 (Portfolio), so there is never zero rows. |
| cli `metricsOutput.ts` | 593 | `owner.today !== undefined` → `true` | Defensive | The CLI always passes `today`. Only a direct caller could leave it out. |
| client `index.ts` | 2957 | `{ method: "GET" }` → `{}` | Equivalent | GET is `fetch`'s default method. |
| mcp-core `index.ts` | 1818 (2) | `now === null` → `false`, or its block emptied | Equivalent | The summariser then throws. `summaryOrNull` catches that and leaves the summary out, which is what `null` does. |
| mcp-core `index.ts` | 1845 | `teamRead?.ok` → `teamRead.ok` | Defensive | `readForSummary` returns `undefined` only when `getTeam` rejects, and the client never rejects. |
| mcp-core `index.ts` | 1884 | `entries === null` → `false` | Equivalent | Same as line 1818: the throw is caught and the summary is left out. |
| mcp-core `index.ts` | 1920 | `chart === null` → `false` | Equivalent | Same as line 1818: the throw is caught and the summary is left out. |
| mcp-core `index.ts` | 2700 | `"portfolio"` → `""` | Equivalent | `readMetricsWording` tests only for `"team"`, so any other text reads the Portfolio's wording. |
