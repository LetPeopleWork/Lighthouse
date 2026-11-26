# Mutation testing — 6705 (Total Work Item Age Over Time shows NaN)

Run 2026-11-26 against the candidate @ `c06d273cf` (production code unchanged since). Gate is 80 % kill rate on
each stack the fix touched, judged on the changed lines.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.1.0), whole files | 73.33 % | 45 | 33 | 12 | 0 | 2 m 58 s |
| Backend, the changed lines | **100 %** | 6 | 6 | 0 | 0 | (same run) |
| Frontend (StrykerJS 10.1.1), changed ranges | **92.31 %** | 13 | 12 | 1 | 0 | 17 s |

Configs: `stryker.6705.backend.json`, `stryker.6705.frontend.json`, `vitest.stryker.mutation.ts` (copied into
`Lighthouse.Frontend/` for the run and removed afterwards).

## Backend

Stryker.NET mutates whole files only, so both changed files were mutated in full: `WorkItemDto.cs` (124 lines, one
changed) and `WorkItemBase.cs` (207 lines, about 20 changed). Unit tests only: `WorkItemDtoTests`, the `WorkItemBase*`
tests, `WorkItemAgeAsOfDateTest` and the instance-day tests; no web host.

| file | changed-line mutants | killed |
| --- | --- | --- |
| `WorkItemBase.cs` `AgingStart` (line 112) | 3 (null-coalescing) | 3 |
| `WorkItemBase.cs` `StartedDateFor` (line 122) | 3 (conditional true/false, equality) | 3 |
| `WorkItemDto.cs` line 49 | 0 (a plain call; its behaviour is mutated in `StartedDateFor`) | — |

### Accepted survivors (all in lines this fix did not change)

- `WorkItemDto.cs` 47, 48, 57 — the `stateAsOf` fallbacks for state, category and state-entered time, and 53 — the
  `asOf` branch of the age. They belong to the historical view; no unit test in this scope builds a DTO with
  `stateAsOf`. Older code, outside this bug.
- `WorkItemBase.cs` 22–30 — default values (`string.Empty`) of `ReferenceId`, `ParentReferenceId`, `Type`, `State`;
  no reader tells an empty default from another string before the first sync sets it.
- `WorkItemBase.cs` 97 — `closedDay >= startDay` to `>`: equivalent, because a same-day close gives cycle time 1 on
  both branches.
- `WorkItemBase.cs` 134 — `startDay <= today` to `<`: equivalent, because an item started today has age 1 on both
  branches.
- `WorkItemBase.cs` 200 — the `AdditionalFieldValues.Clear()` in the copy method; older code, outside this bug.

## Frontend

`mutate` names the changed ranges: `totalAgeOfStartedItems.ts:6-16` and `TotalWorkItemAgeRunChart.tsx:50-65`.

### Accepted survivor

- `TotalWorkItemAgeRunChart.tsx:56` — the `{ month: "short", day: "numeric" }` options of the day label became `{}`.
  That line was not changed by this fix; it lies inside the range only because the range spans the loop body. The
  label format is not part of the promise.
