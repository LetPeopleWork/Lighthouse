# A named Cycle Time shows its own Work Items (ADO Bug #6656)

Delivered 2026-11-24. Workspace: `docs/feature/bug-6656-named-cycle-time-own-population/` (`brief.md`,
`architecture.md`, `acceptance.md`, `feature-delta.md`, `runs/`, `mutation/`).

## What was delivered

With a named Cycle Time selected (for example Analysis → Ready for Release), the Cycle Time Scatterplot showed the
same Work Items as the default Cycle Time, the ones closed in the dates, with recalculated values. Now a named
Cycle Time answers from its own Work Items: every Work Item whose last move into the measure's end state falls in
the selected dates, closed or not. Each one is placed on the day it reached the end state; a Work Item that never
reached it is not shown. The same Work Items drive the percentiles, the Cycle Time Percentiles widget (current
and previous period), its View Data list and the outliers. Team and portfolio. The default Cycle Time is unchanged.

Visible changes for a named measure: the x-axis reads "Date reached <end state>", the dot dialog is titled
"<Work Items> that reached <end state> on <date> with <Cycle Time> of N days" and shows that measure's Cycle Time,
and the percentiles View Data is titled "<Work Items> that reached <end state>". While the measure loads, the
previous dots are dimmed under a spinner; if it cannot be loaded, the chart says so.

API: `cycleTimeData` on the team and portfolio metrics routes takes an optional `definitionId`; each named value
in the answer carries `endDate`. Without the parameter the answer is as before. No migration, no setting.

## Slices and key commits

| Slice | What it delivers | Key commits |
|---|---|---|
| S1 | Backend: one population rule for every named answer; `definitionId` on `cycleTimeData`; `endDate` per named value | `145bd2b8a`, `a7345ab88`, `0859cf776`, `ecbd7b7ff`, `e72f5beea` |
| S2 | Scatterplot, dot dialog and percentiles View Data use the named measure's own Work Items | `3ceb660fb`, `7de9adb1b`, `8c976b35a`, `1fe57a116`, `856c55df4`, `7ead089ae`, `dfd1e15ab` |
| Final gates | Mutation kill-round tests; a stale View Data list on a switch between named measures fixed in place | `3ef030881`, `1a9c63cd0`, `e1b84fecd`, `6286a444a` |

## Gates

- Whole-candidate review: rejected once (nothing proved the controllers pass `definitionId` on), accepted after
  the revision at `6a2d4e59d`. EXAMINE: accepted, E1–E6 met (from test probes; no live run).
- Full gates: `pnpm test` 695 files / 10,289 passed; `dotnet test` (live tracker categories excluded) 14,334
  passed; both builds clean.
- Mutation: frontend 92.86 %, backend 93.02 % on the changed lines (see `mutation/results.md`).

## Knowingly left open

- No live run of the reporter's case: the demo data has no named Cycle Time. Worth one manual check.
- On a switch between two named measures, the percentile numbers of the earlier measure stay until the new ones
  arrive. This predates the bug; the View Data list no longer does it.
- The Work Item Age chart does not follow a named Cycle Time (out of scope, per the reply to the reporter).

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done. `docs/metrics/flow-metrics.md`, "Named Cycle Times (Premium)": what a named cycle time plots (its own Work Items, on the day they reached the end state, closed or not), the x-axis wording, and that the percentiles widget and its View Data follow. Default terms used. |
| Screenshots | Deferred to the next `/update-docs` (or the maintainer's word): `docs/assets/features/metrics/cycleScatterNamedCycleTime.png` now shows the old x-axis wording. Test: "Take @screenshot of named cycle times on the charts and the config editor" in `Lighthouse.EndToEndTests/tests/specs/screenshots/Screenshots.spec.ts`; run it alone with `--grep`. Needs a clean backend with a premium licence. |
| Demo data | N/A, because the fix changes which Work Items a named Cycle Time shows, not what data exists; the demo data has no named Cycle Time, and adding one is not part of a bug fix. |
| Website assets | N/A for this commit: no asset under `docs/assets/` changed. The screenshot re-run above will change `cycleScatterNamedCycleTime.png`; check then whether the website hot-links it. |
| Clients (CLI, MCP) | N/A, because the clients call `cycleTimeData` without `definitionId` and never read `namedCycleTimes`; `endDate` is additive (checked in `/storage/repos/lighthouse-clients` by the whole-candidate reviewer). |
| Context map, `ARCHITECTURE.md` | N/A, because no concept, context or edge is added; Flow Metrics only. |
| Usage-data event | N/A, because DEVOPS decided so: a bug fix of an existing view, nothing new to count. |
| Mutation | Done. Frontend 92.86 % (143/154), backend 93.02 % on the changed lines (40/43; whole files 49.66 %, the rest is code this bug did not touch). Survivors triaged in `mutation/results.md`. |
| Release notes | N/A; written at /release. |
