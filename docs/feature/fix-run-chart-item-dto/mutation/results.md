# Mutation testing — 5857 (Work Item Age/Cycle Time empty in some Work Item Dialog Instances)

Run 2026-08-31 against `main` @ `8214ba949`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) — whole file | 57.83 % | 305 | 240 | 65 | 0 | 3 m 44 s |
| Backend — lines this change touched | **100 %** | 13 | 13 | 0 | 0 | (same run) |
| Frontend (StrykerJS 9.6.1) | **100 %** | 32 | 32 | 0 | 0 | 53 s |

Configs: `stryker.5857.backend.json`, `stryker.5857.frontend.json`, `vitest.stryker.mutation.ts`.

The two backend numbers come from one run. Stryker.NET ignores line ranges, so the two metrics
controllers are mutated whole even though this change rewrote only their run-chart actions. The
whole-file score is therefore a measure of two 700-line controllers that mostly predate this fix; the
second row counts only the mutants landing on lines the diff touched, which is what this gate is
about.

## Backend

| file | tested | killed | survived | survivors on changed lines |
| --- | --- | --- | --- | --- |
| `API/DTO/RunChartDataDto.cs` (new) | 11 | 11 | 0 | 0 |
| `API/TeamMetricsController.cs` | 147 | 120 | 27 | 0 |
| `API/PortfolioMetricsController.cs` | 147 | 109 | 38 | 0 |

### Closed by this pass

`RunChartDataDto.cs:109` — `OwningTeam = feature?.OwningTeam ?? string.Empty`. Replacing the fallback
string survived while removing the null-coalescing operator was killed: the suite pinned the case
where the item really is a Feature and left the fallback — the one the line exists for — unasserted.
`RunChartPayloadContractDtoTest` now builds the portfolio item DTO from a plain work item and pins
the empty owning team and zero size. That fallback guards the declared-type flattening that lost
`size` and `owningTeam` from the payload in the first place, so an unpinned fallback was the same
hole the bug came through.

### Accepted survivors

The 65 survivors all sit in controller actions this change did not touch — the percentile, process
behaviour, forecast and settings endpoints of the two metrics controllers. They are pre-existing test
debt, unrelated to the run-chart payload, and closing them means writing tests for eight other
features. Recorded here rather than fixed.

### Not mutated

Nothing was excluded. The `mutate` list is the three production files the fix changed; the test-case
filter is the four test classes covering them (215 tests), which keeps the initial run under a minute
instead of executing all 6318.

## Frontend

| file | tested | killed | survived |
| --- | --- | --- | --- |
| `pages/Common/MetricsView/BaseMetricsView.tsx:166-205` | 26 | 26 | 0 |
| `components/Common/Charts/ThroughputChart/evaluateCondition.ts:22-28,98-116` | 6 | 6 | 0 |

### Closed by this pass

`BaseMetricsView.tsx:200` — emptying the loop that writes the size-chart features into the lookup
survived. That loop is the reason a Feature outranks every run-chart record for the same id, which is
what the estimation-versus-cycle-time drill-through reads. A new case in the `buildWorkItemLookup`
suite pins a size-chart feature overriding a cycle time record for the same id.

### Accepted survivors

None.
