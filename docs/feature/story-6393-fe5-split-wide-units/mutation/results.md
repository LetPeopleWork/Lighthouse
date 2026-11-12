# Mutation testing — Story 6393 part A (FE5, split wide frontend units)

Run 2026-11-12 on `main` @ `1568c69f5` (part A's code frozen after its final refactor), StrykerJS, frontend only.
Gate: 80 % kill rate. Backend: N/A, part A changed no backend file.

| run | score | tested | killed | timeout | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1, plain | **80.26 %** | 1292 | 1034 | 3 | 210 | 45 | 4 m 3 s |
| 2, incremental after one kill round (tests only) | **97.06 %** | 1292 | 1251 | 3 | 38 | 0 | 52 s |

Configs: `stryker.6393a.frontend.json`, `vitest.stryker.6393a.mutation.ts` (copy it into `Lighthouse.Frontend/` to run).
Survivor lists: `survivors-run1.txt`, `survivors-run2.txt`. Logs: `run1.log`, `run2.log`.

## Scope

Mutated against their direct unit tests only:

| file | run 1 | run 2 |
| --- | --- | --- |
| `src/hooks/useScopedAccess.ts` | 81.48 % | 98.77 % |
| `src/pages/Common/MetricsView/dashboardFrame.ts` | 100 % | 100 % |
| `src/pages/Common/MetricsView/workItemLookup.ts` | 96.00 % | 100 % |
| `src/pages/Common/MetricsView/widgetViewData.ts` | 38.46 % | 96.15 % |
| `src/pages/Common/MetricsView/ragRulesOverview.ts` | 96.86 % | 98.74 % |
| `src/pages/Common/MetricsView/ragRulesTimeAndAge.ts` | 86.60 % | 98.20 % |
| `src/pages/Common/MetricsView/ragRulesVolumeAndFlow.ts` | 78.39 % | 95.13 % |

Run 1's low figures for `widgetViewData.ts` and `ragRulesVolumeAndFlow.ts` came from code moved out of
`BaseMetricsView.tsx`, whose older tests are component tests outside this scope. The kill round covered it at unit
level instead, which moves those checks down a layer.

### Not mutated

- `widgetFooters.ts`, `connectionFormRules.ts`, `useConnectionSave.ts` and every `.tsx` file part A split
  (`BaseMetricsView`, `widgetNodes`, `ModifyConnectionSettings` and its sections, the two detail pages). They hold code
  moved without change, and their component tests passed unmodified. Covering them would mean the slow component
  suites, about 1 to 1.5 hours; the maintainer was offered that option and the default (this scope) was taken.
- `blockedMaxAgeRag.ts`: the final refactor only un-exported two types and rewrote two comments; no mutants.

## Closed by the kill round

Tests added to the direct test files (production unchanged):

- `widgetViewData.test.ts`: each list's title in the team's own words, each list's source, started and closed counts
  with arrivals only or throughput only, the estimation list with an unknown id and with no data, the feature lists'
  filter, the SLE on the percentile, scatter and PBC lists, and the named-scope lists.
- `useScopedAccess.test.ts`: initial state, loading while pending, a pending write clears an earlier error, no scope
  does nothing, handlers handed out before the scope was known act on it after a re-render.
- `ragRules*.test.ts`: exact status and tip text and the boundary values of each rule.
- `workItemLookup.test.ts`: no input gives an empty list.

## Accepted survivors (equivalent)

- `useScopedAccess.ts:46` initial ref value: the layout effect sets the ref before any handler can run.
- `ragRulesOverview.ts:8, :95`, `ragRulesTimeAndAge.ts:124`, `ragRulesVolumeAndFlow.ts:9, :102, :192` `<= 0` → `< 0`:
  `!x` already catches 0.
- `ragRulesTimeAndAge.ts:8`: the empty-stats guard repeats the caller's own `totalItems === 0` check, same result.
- `ragRulesTimeAndAge.ts:268`: (0, 0) is handled one branch earlier; only negative ages, which cannot occur, differ.
- `ragRulesVolumeAndFlow.ts:41`: unreachable, the totals differ by at least 2 there.
- `ragRulesVolumeAndFlow.ts:69, :389` `length + 3`: windows past the end hold `undefined`, which never counts.
- `ragRulesVolumeAndFlow.ts:236, :244` → `true`: `s > undefined` is false either way.
- `ragRulesVolumeAndFlow.ts:279` `new Array()`, `:281` `i - 1`: arrays grow; a constant rank shift keeps every difference.
- `ragRulesVolumeAndFlow.ts:306` `rho > 0.3`: a computed rho of exactly 0.3 is `0.30000000000000004`.
- `ragRulesVolumeAndFlow.ts:365` (5 mutants): this guard repeats the one inside `computeStartedVsClosedRag`, same red
  tip. A cleanup candidate; removing it is a production change, so not done here.
- `ragRulesVolumeAndFlow.ts:380` (3), `:385` (2): the red pass-through equals the final return; the loop is empty for
  empty arrays.
- `widgetViewData.ts:119` optional chaining on `dataPoints`: differs only for an input the type does not allow.

## Survivors left open

Not triaged as equivalent; worth a test when the area is next touched:

- `ragRulesVolumeAndFlow.ts:237` `s > p85` → `>=`, `:245` `s > p70` → `>=`: the value exactly at the percentile.
- `ragRulesTimeAndAge.ts:307` → `false`.
- `widgetViewData.ts:107` → `true`, `:127` (2), `:192` string literal.
