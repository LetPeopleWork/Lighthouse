# Bug 6700 — several charts list the same Work Item many times

ADO: Bug #6700 (Closed, tagged Release Notes). Delivered 2026-11-25 in AFK mode, rigor profile `standard`.

## What was delivered

The **View Data** list of Work Items In Progress Over Time, Total Work Item Age Over Time, and the Work In Progress
and Total Work Item Age Process Behaviour Charts listed a Work Item once for every day it was in progress, so an item
in flight for 20 days of the range showed up 20 times. The run chart groups its Work Items by day, and the list joined
all days without removing repeats. It now lists each Work Item once, in the order of the days. The throughput,
arrivals and started-and-closed lists read the same shape and leave out repeats too; their content does not change,
because a Work Item starts or closes on one day.

The repeats came in with the View Data button (`4998f2eb7`, 2026-04-06); nothing that removed them was dropped later.

## Commits

- `91a9296f4`, `17f6bfc21` — the oracle `widgetViewData.repeatedItems.test.ts` (RED 16/16 before the fix), revised
  after oracle review round 1 (Work Items from other sources stay out; a second range).
- `7d079d494` — the fix in `workItemLookup.ts`.
- `c1f8a8222` — refactor: one de-duplication rule shared with the Work Item lookup.
- `4536497ed` — Biome formatting of the oracle; the oracle was re-approved on its new bytes.
- `5d876538f`, `6240b7c9a`, `5c4083224` — review records, the EXAMINE waiver and the mutation record.

Gates: `pnpm build` clean; `pnpm test` 10379 passed (the per-test time budget flagged 15 unrelated tests under load;
re-run alone, 0 offenders); whole-candidate review accepted; Stryker 7/7 killed (100 %); CI runs 45662084827 and
45665611862 green.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because `docs/metrics/widgets.md` already says View Data shows "the full set of work items that feed the widget"; the fix makes the four lists match it. |
| Screenshots | N/A, because no `@screenshot` test opens the View Data list of these four charts, and no layout or copy changed. |
| Demo data | N/A, because there is nothing new to show. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because the API is unchanged; the defect was in the frontend list. |
| Context map, `ARCHITECTURE.md` | N/A, because no concept, context or edge changed. |
| Usage-data event | N/A, because a bug fix adds nothing new to count. |
| Mutation | Frontend 100 % (7/7) on the changed lines; backend N/A, no backend file changed. See `docs/feature/bug-6700-chart-data-repeats-items/mutation/results.md`. |
| Release notes | N/A; written at /release. |

## Left open

- **EXAMINE waived:** the examiner has no public probe that opens a chart's data list (same gap as Bug 6698 and
  Story 6648). The real-app check is the maintainer's own look.
- **Separate Bug to raise:** the reporter also saw nothing under Total Work Item Age Over Time. Not investigated here.
