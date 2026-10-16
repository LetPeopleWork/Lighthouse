# Mutation testing — 6247 (Lighthouse marks stale on the server)

Run 2026-10-16 against `main` @ `058125d02` (local commits, not yet pushed). Gate is 80 % kill rate on
both stacks.

| stack | score | tested | killed | survived / no cov. | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), whole files, first run | 63.84 % | 354 | 226 | 103 / 25 | 0 | 8 m 58 s |
| Backend, whole files, after the kill round | 64.59 % | 353 | 228 | 100 / 25 | 0 | 3 m 57 s (narrowed) |
| Backend, this story's changed lines, first run | 84.62 % | 39 | 33 | 6 / 0 | 0 | — |
| **Backend, this story's changed lines, after the kill round** | **92.31 %** | 39 | 36 | 3 / 0 | 0 | — |
| Frontend (StrykerJS 10.0.0), first run | 92.31 % | 52 | 48 | 4 / 0 | 0 | 3 m 18 s |
| **Frontend, after the kill round** | **98.08 %** | 52 | 51 | 1 / 0 | 0 | 1 m 28 s (incremental) |

Configs: `stryker.6247.backend.json` (the full run), `stryker.6247.backend.kill.json` (the narrowed
re-check), `stryker.6247.frontend.json`, `vitest.stryker.mutation.ts`.

## Backend

Run from `Lighthouse.Backend/Lighthouse.Backend.Tests/`:
`dotnet stryker --config-file ../../docs/feature/story-6247-server-marks-stale/mutation/stryker.6247.backend.json`.

Stryker.NET mutates whole files. The policy, its verdict and the reason DTO are new, but
`WorkItemService` is 1340 lines of which the story changed about twenty, so the whole-file score is
dominated by survivors in sync code the story did not touch. The gate is judged on the story's changed
lines: every mutant whose span touches a line added or changed in `git diff origin/main..058125d02`,
which for the new files is every mutant.

| file | whole-file score (first run → after) | story-line mutants | story-line survivors (first run → after) |
| --- | --- | --- | --- |
| `Models/Staleness/StalenessPolicy.cs` (new) | 93.55 % → 96.77 % | 31 | 2 → 1 |
| `Models/Staleness/StalenessVerdict.cs` (new) | 100 % | 1 | 0 |
| `API/DTO/WorkItemDto.cs` | 55.56 % → 44.44 %* | 1 | 1 → 0 |
| `Services/Implementation/WorkItems/WorkItemService.cs` | 61.02 % → 61.98 % | 6 | 3 → 2 |
| `Models/Staleness/StaleReasonKind.cs` (new) | no mutants (enum) | 0 | — |
| `API/DTO/StaleReasonDto.cs` (new) | no mutants (positional record) | 0 | — |

\* The narrowed re-run leaves out the controller and `FeatureDto` tests, which killed two
`stateAsOf ?? workItem` mutants on lines 47 and 48 in the first run. Those lines predate the story;
the drop is the narrower filter, not a regression.

Unit tests only: `StalenessPolicyTest`, `WorkItemDtoTests`, `FeatureDtoTest`, `TeamMetricsControllerTest`,
`PortfolioMetricsControllerTests` and `WorkItemServiceTest`. The story's acceptance suites under
`API.Integration.Staleness` (`StaleWorkItemsAcceptanceTest`, the `Slice01ServerMarksStale*` and
`Slice01StaleSignal*` scenarios) boot the whole app and are left out by `!~API.Integration`.
The first-run survivors that only they reached are closed below with unit tests.

The survivors and no-coverage mutants left in the whole-file figures for `WorkItemService` (94 and 25)
all sit on lines this story did not change: duplicate-reference logging, delta-sync mode selection,
feature size defaults, blocked-transition events for Features, and log-message strings. They predate
the story and are not triaged here.

### Closed by this pass

- `WorkItemService.cs:507`, `isBlocked ? CurrentStateEnteredAt : null` to always `null`: the sync
  would hand the policy no blocked start, so a Blocked item could never go stale on its blocked time.
  New `WorkItemServiceTest` case: a Work Item Blocked for 10 days with a blocked threshold of 3 and a
  time-in-state threshold of 30 publishes `WorkItemBecameStale` once, with threshold 3.
- `StalenessPolicy.cs:52`, `ThresholdCrossedBy` always answering with the time-in-state threshold.
  Killed by the same sync test, and pinned directly by the new `StalenessPolicyTest` case "a stale
  Work Item crossed the threshold of the reason that made it stale" (blocked → 3, in its state → 5).
- `WorkItemDto.cs:116`, emptying `CarryStaleness`. Only the acceptance suite read `IsStale` and
  `StaleReasons` before; the new `WorkItemDtoTests` case judges a Blocked DTO and pins both reasons.

All three were confirmed killed by the narrowed run (`stryker.6247.backend.kill.json`).

### Accepted survivors (this story's lines)

- `StalenessPolicy.cs:49`, the `ArgumentException` message as `""`. The test pins the exception
  type; the wording is not behaviour.
- `WorkItemService.cs:497-499`, removing the early `NotStale` for an item outside Doing or with no
  state entry. The policy answers `NotStale` for exactly those items too (no candidate outside Doing
  is stale, and with no entry date there is neither a time in state nor, since the blocked start is
  the same date, a blocked time). The guard only spares a blocked-rule evaluation. Equivalent. The
  `||` to `&&` mutant on line 496, which the story did not change, is equivalent for the same reason.
- `WorkItemService.cs:507`, `isBlocked ? CurrentStateEnteredAt : null` to always the entry date.
  An unblocked candidate is judged on its time in state alone and never reads `BlockedSince`.
  Equivalent.

### Not mutated

- `API/TeamMetricsController.cs` (667 lines, +12) and `API/PortfolioMetricsController.cs` (681 lines,
  +34): most of each change is reworded comments and an `asOfDay` local; the behaviour is one
  `StalenessPolicy.For(owner)` and one `dto.CarryStaleness(...)` call per WIP read. Mutating either file
  would bury those few mutants under hundreds on unrelated endpoints. The calls are covered by the
  story's acceptance suite, which reads `/teams/{id}/metrics/wip` and `/portfolios/{id}/metrics/wip`
  as of a chosen day and asserts `isStale` and `staleReasons`, and the verdict they carry is pinned
  by the `WorkItemDtoTests` case above.

## Frontend

Line ranges of the story's change only (`git diff -U0 origin/main..058125d02`), `coverageAnalysis: perTest`
with `--incremental`. Copy `vitest.stryker.mutation.ts` into `Lighthouse.Frontend/` before a run, then
from there: `pnpm exec stryker run ../docs/feature/story-6247-server-marks-stale/mutation/stryker.6247.frontend.json --incremental`.

| file | mutants | killed (first run → after) | survived (first run → after) | score after |
| --- | --- | --- | --- | --- |
| `components/Common/TimeInStateBadge/TimeInStateBadge.tsx` (whole file) | 35 | 32 → 34 | 3 → 1 | 97.14 % |
| `components/Common/Charts/WorkItemAgingChart.tsx` | 6 | 5 → 6 | 1 → 0 | 100 % |
| `pages/Common/MetricsView/BaseMetricsView.tsx` | 11 | 11 | 0 | 100 % |
| `components/Common/WorkItemsDialog/workItemColumns.tsx` | 0 | — | — | — |

`workItemColumns.tsx` changed two JSX props passed straight through (`isStale={row.isStale}`,
`staleReasons={row.staleReasons}`), which StrykerJS does not mutate. `WorkItemsDialog.tsx` only lost
two optional fields of a type, and `models/WorkItem.ts` gained types only; neither was put in `mutate`.

### Closed by this pass

- `WorkItemAgingChart.tsx:481`, the grouping memo's dependencies as `[]`: the chart would keep the
  dots of the first answer it was given. New spec "reds the dot once a fresh answer marks the Work
  Item stale" renders a not-stale item, re-renders with the server's stale answer, and pins the red.
- `TimeInStateBadge.tsx:18`, the `null` guard in `daysInState` (condition `false`, block emptied).
  The badge returns early on `null`, but the Work Items dialog sorts its rows by `daysInState` with
  `currentStateEnteredAt ?? null`. New spec: an unknown state entry counts no days.

### Accepted survivors

- `TimeInStateBadge.tsx:46`, the `staleReasons = []` default as `["Stryker was here"]`. The reasons are
  filtered by their `kind` against the three known kinds, and a string has no `kind`, so it drops
  out and the tooltip reads the same. Equivalent.

`git status` was clean of mutants after both runs.
