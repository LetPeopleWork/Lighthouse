# Bug #5857 — Work Item Age and Cycle Time blank in the Work Item dialog

**Shipped** 2026-08-31 · ADO Bug #5857 · tagged `Release Notes` · workspace `docs/feature/fix-run-chart-item-dto/`

## What users saw

The shared Work Item dialog rendered Cycle Time and Work Item Age as empty cells when opened from the
Throughput run chart, the Arrivals run chart, Work In Progress over Time and the "View Data" table. The
same dialog opened from the Cycle Time scatter chart was fine, which is what made it look like a display
bug in particular charts.

## Root cause

`WorkItemBase` was simultaneously the EF entity and the wire contract for seven metrics endpoints.
Bug #5567 (`8c00e49b2`) converted `CycleTime` and `WorkItemAge` from properties into methods taking a
time zone and a calendar day — correct for its own purpose, since a property cannot take a parameter, but
System.Text.Json cannot serialise a method. Both fields silently left every response that returned the raw
entity. First release carrying the regression: v26.8.1.14. The path that worked returned `WorkItemDto`.

The blast radius was wider than the report:

- `isBlocked` was gone from every affected dialog, so the blocked marker never drew.
- On portfolios `size` and `owningTeam` were gone too — `List<WorkItemBase>` serialises by declared type,
  flattening `Feature` to its base, which silently removed the dialog's "Owned by" column.
- The process behaviour drill-throughs and Estimation vs Cycle Time were broken and unreported, because
  `buildWorkItemLookup` seeded from run-chart items with an unconditional `set` and added the richer
  DTO-backed sources only `if (!has)` — the impoverished record won.
- The CSV export carried the blank column, and 11 of the 25 "View Data" payloads were affected.

Confirmed empirically rather than inferred: a scratch console app serialised a real `RunChartData` against
the built assembly with the production converter set, and the emitted key set carried no `cycleTime`, no
`workItemAge`, no `isBlocked`.

## What was built

`RunChartDataDto<TWorkItem>` at the boundary, mapping all seven endpoints. The envelope is generic rather
than holding a common base type because a JSON writer emits collection elements by the type the collection
declares, so a bucket declared as the base would drop whatever a richer item adds.

| Commit | Change |
| --- | --- |
| `ded0bd140` | Team throughput/arrivals/wipOverTime return the DTO |
| `f5d3451f9` | The four portfolio endpoints return the DTO |
| `4287ddd41` | `buildWorkItemLookup` prefers the richer record |
| `df7e42754` | Roadmap + RCA |
| `c3cadaa8b` | Name the order the lookup trusts its sources in |
| `774a72261` | Stop reporting an age for items that closed inside the range |
| `83d7700c2` | Read the parent reference by the name the payload uses |
| `8214ba949`, `7a8b9c36b` | Tests closing two mutation survivors |

## Decisions worth keeping

**`Size` and `OwningTeam` stay off the shared item DTO.** `hasOwningTeams` in `WorkItemsDialog.tsx` is an
`"owningTeam" in item` test, so putting them on the shared DTO would give the *team* dialog a permanently
empty "Owned by" column. Hence the generic envelope with `PortfolioRunChartWorkItemDto` restating the two
fields.

**`RunChartWorkItemDto` must keep `Tags` and `AdditionalFieldValues`.** `evaluateCondition.ts` declares
them as the rule-evaluable surface of the premium throughput filter. `evaluateConditions` has no production
caller today, so a plain `WorkItemDto` swap would go green and demolish that contract later.
`ForecastFilterThroughputChartIntegrationTest` is the canary.

**Age anchoring (maintainer, 2026-08-27).** `wipOverTime` passes `asOf = endDate`, matching `/wip` and the
aging chart. Throughput and arrivals stay today-anchored.

## Two further defects found while fixing

Both came out of manual verification, after the roadmap was complete:

1. **Work in progress over time reported an age for items that closed inside the range** (`774a72261`).
   The endpoint deliberately includes items that were open on any day of the range, so anchoring age on the
   last day counted the days *since an item finished*: one that lived five days and closed six days before
   the range ended reported eleven. That is worse than the blank cell this branch set out to fix — a
   confidently wrong number in the Total Work Item Age table and its CSV export. Only an item still open is
   anchored to a day now, and the guard sits on the DTO because both charts ask the same question.

2. **`evaluateCondition.ts` read `parentReferenceId`** (`83d7700c2`), which is what the raw entity used to
   put on the wire; every `WorkItemDto` endpoint calls it `parentWorkItemReference`. The null guard let
   `undefined` through into `toLowerCase`, so a rule on the parent reference would crash rather than merely
   stop matching. Nothing hits it today, which is exactly why it would have been found late and elsewhere.

## Quality gates

Backend 6319 tests green, 0 warnings; frontend 4640 tests, clean build and Biome. CI green on `7a8b9c36b`,
`sonar-gates` included.

Mutation testing scored **100 % on the lines this change touched** — backend 13/13, frontend 32/32
(`docs/feature/fix-run-chart-item-dto/mutation/results.md`). The whole-file backend figure is 57.83 %
because Stryker.NET cannot mutate a line range and the two large metrics controllers are therefore measured
whole; those survivors are pre-existing debt in untouched endpoints.

Two survivors were real and produced tests. The `?? string.Empty` owning-team fallback was unpinned — the
suite covered the case where the cast succeeds and not the case the line exists for — and the loop giving
size-chart features precedence in `buildWorkItemLookup` could be emptied without any test noticing.

## Lessons

- **An entity used as a wire contract has no compiler telling you when the contract changes.** Turning a
  property into a method removed a field from seven responses with no error anywhere. The prevention item
  that would have caught it is an ArchUnit rule forbidding a controller action from exposing a type
  reachable from the `DbSet`s.
- **A test that pins the happy branch of a fallback pins nothing.** Both mutation survivors were of that
  shape: the assertions covered the path where the value is present and left the defensive path — the
  reason the line was written — unasserted.
- **Presence assertions are not value assertions.** The regression test deliberately checks that a closed
  item's cycle time equals the inclusive day span, and that an item appearing in two endpoints reports the
  same value, because a mapper hardcoding `0` would pass presence checks.

## Follow-ups, deliberately not done here

- **`@screenshot` re-shoot** for the portfolio run-chart dialogs, which now regain the "Owned by" column.
  Deferred by the maintainer.
- Prevention items from the RCA: a zod `WorkItemSchema` on the run-chart fetchers, the ArchUnit rule above,
  a `ci-learnings.md` entry, and optional `cycleTime`/`workItemAge` in TypeScript so `tsc` forces every
  `valueGetter` to handle absence.
- `PortfolioMetricsController`'s `/started` has no frontend consumer. Fixed with the rest rather than left
  leaking the shape, but it is a dead endpoint.
