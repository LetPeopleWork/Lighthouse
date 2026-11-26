# Bug 6705 — Total Work Item Age Over Time shows NaN and no line

ADO: Bug #6705 (tagged Release Notes, reported by a community member on Slack). Delivered 2026-11-26, partly in AFK
mode, rigor profile `standard`.

## What was delivered

For one team, the **Total Work Item Age Over Time** chart drew no line, and its tooltip read "NaN days total (40
items)". The details list was right. One Work Item in progress had no start date. The server counts such an item as in
progress from its created date, but it sent the item to the client with an empty start date. The chart added up
the ages itself, the missing date made one age NaN, and that turned every day's total into NaN.

- **Server:** a Work Item in a Doing state with no start date is now sent with its created date as its start date,
  the same date the server already ages it from. To Do items still have no start date. The rule now has one owner on
  the Work Item (`StartedDateFor`), which the age and the age on a given day read too.
- **Chart:** if a start date is still missing, the chart leaves that Work Item out of the day's total and item count
  and draws the others. It never shows NaN.

Why a Work Item in progress can have no start date is still open. Lighthouse dates the start from the last move into
a Doing state in the item's history. For the reporter's Jira Data Center item, which did start in "Open", the likely
causes are a status name in the history that no longer matches the team's Doing states, or a path through a state
mapped as Done. The reporter was asked for that item's changelog.

## Commits

- `7c8810491` — the oracles: `WorkItemDtoTests` (RED: expected the created date, was null) and
  `TotalWorkItemAgeRunChart.test.tsx` (RED: `[NaN, NaN]` instead of `[4, 6]`).
- `61c004f77` — fix: the server sends the created date as start date for an in-progress item without one.
- `4f969e278` — fix: the chart skips an item without a start date.
- `1cfccfe1b` — Biome rewrap of one oracle line; the oracle was re-approved on its new bytes.
- `d69662501`, `c06d273cf` — refactors after the candidate review: the day total as a pure function with its own
  tests (added to S2's oracle by the maintainer's decision), and one owner for the "aged from" date.
- `ac789c37b`, `859d6f15f`, `0f45c9c20`, `320fbf335` and the review commits — nwdes records, acceptance rows, mutation
  results.

Gates on `c06d273cf`: `pnpm build` clean; `pnpm test` 10384/10384 passed (the per-test time budget flagged 1–2
untouched tests on a machine at load 10–16; each passes alone with 0 offenders; the maintainer accepted this
evidence and left the timing check to CI); `dotnet build` 0 warnings; `dotnet test` (live connectors excluded)
15304/15304. Whole-candidate review accepted (after one revision pass). EXAMINE: no admitted public probe shows a
Work Item's start date from the API or renders this chart, so E1 and E2 were recorded as observation gaps; the
maintainer waived both (coverage: the approved oracles, and the maintainer's own look at the real app after the
push), and the examination closed accepted on that waiver, not on an observation.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because `docs/metrics/flow-metrics.md` describes the chart as the total age of the Work Items in progress, which is what it shows again; it says nothing about start dates. |
| Screenshots | N/A, because no layout or copy changed; the chart looks as it always did once its line is drawn. |
| Demo data | N/A, because there is nothing new to show. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because the API shape is unchanged; `startedDate` now carries a value for an in-progress item that had none, which clients already accept. |
| Context map, `ARCHITECTURE.md` | N/A, because no concept, context or edge changed. |
| Usage-data event | N/A, because a bug fix adds nothing new to count. |
| Mutation | Backend: the changed lines 6/6 killed (whole files 73.33 %, every survivor in untouched code); frontend 92.31 % on the changed ranges. See `docs/feature/bug-6705-total-age-nan/mutation/results.md`. |
| Release notes | N/A; written at /release. |
