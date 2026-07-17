# Slice 02 - Trend follows the named cycle-time selection

**Type:** vertical | **Est:** ~1 day | **Stories:** US-03

## Learning hypothesis

The previous-period trend generalises to a named cycle time by threading an optional `definitionId`
through `cycleTimePercentilesInfo` and calling the shipped `GetNamedCycleTimePercentilesForTeam` twice
(current + previous period) - exactly as the default path already calls
`GetCycleTimePercentilesForTeam` twice (`TeamMetricsService.cs:693-697`) - with no new DTO and no new
error contract.

**Disproves if it fails:** that the Info endpoint's shape generalises. If a named trend needs its own
DTO or its own error semantics, the "named is just the default with a different boundary pair"
premise that Epic 5251's D1 rests on is weaker than believed, and the remaining named surfaces
(`cycleTimePbc`, per-definition SLE) get more expensive.

**Confirms if it succeeds:** any percentile-derived Info endpoint can take a `definitionId` the same
way, making future named surfaces mechanical.

## What ships

- **Backend**: `[FromQuery] int? definitionId = null` on `GetCycleTimePercentilesInfo` (Team +
  Portfolio controllers), routed through the same `IsNamedRequest(definitionId)` idiom the sibling
  `cycleTimePercentiles` action already uses (`TeamMetricsController.cs:140-151`). Service gains a
  named path that calls `GetNamedCycleTimePercentilesForTeam` for the current and previous window and
  feeds the existing `BuildCycleTimePercentilesInfoDto`. **Cache key gains a definition segment**
  (see below). Portfolio twin mirrors it.
- **Frontend**: `getCycleTimePercentilesInfo(id, start, end, definitionId?)` gains the suffix exactly
  as `getCycleTimePercentiles` does (`MetricsService.ts:332-339`); the trend payload for the
  `percentiles` widget follows slice 01's lifted selection.

## The trap this slice must not step in

`GetCycleTimePercentilesInfoForTeam` caches under `CycleTimePercentilesInfo_{start}_{end}`
(`TeamMetricsService.cs:691`) - **no definition segment**. Adding `definitionId` without extending the
key makes the named trend and the default trend collide for the same date range: first caller wins,
second silently receives the other's numbers. The shipped named percentile method already gets this
right (`NamedCycleTimePercentiles_{start}_{end}_Def_{definitionId}`, `TeamMetricsService.cs:344`) -
mirror it. Covered by an explicit AC, not left to review.

## IN scope

- Optional `definitionId` on the Info endpoint (Team + Portfolio), named service path, cache-key
  segmentation, FE wiring to slice 01's selection.
- Backend NUnit tests incl. the collision case; FE tests for the named trend footer.

## OUT of scope

- Any change to the default trend's behaviour or contract (must stay byte-identical).
- `cycleTimePbc`, `workItemAgePercentiles`, per-definition SLE.
- The pre-existing `lighthouse-clients` version-gate gap on `getTeamCycleTimePercentiles`
  (feature-delta Cross-cutting) - a separate bug, not this slice.

## Production-data AC

Driven from real demo data (`DemoDataFactory.cs:74`: `Lead Time (End to End)` = Backlog->Done,
`Analysis to Done` = Analysing->Done, on both demo Team and Portfolio). Use the real names in
fixtures - "Concept to Cash" is Epic 5251 narrative shorthand only.

- Given the demo Team with `Lead Time (End to End)` selected on Flow Overview, when the widget loads,
  then the trend footer compares that definition's current-period percentiles against its OWN
  previous-period percentiles (e.g. "down 5 days vs previous period").
- Given a named trend and a default trend are requested for the SAME entity and SAME date range, when
  both are served, then each returns its own answer - they do not collide in cache. **This AC exists
  because the current key would collide; assert the two differ, do not merely assert each is
  non-null.**
- Given `Lead Time (End to End)` and `Analysis to Done` trends are requested for the same range, when
  both are served, then each returns its own definition's comparison (the key segments by definition,
  not just by named-vs-default).
- Given "Default" is selected, when the trend footer renders, then it is byte-identical to today.
- Given `definitionId` names a non-existent or invalid definition, when the endpoint is called, then it
  behaves exactly as the sibling `cycleTimePercentiles` named path does for the same input.
- Given the demo Portfolio with `Lead Time (End to End)`, when its named trend is requested, then it
  behaves identically to Team scope.

## Dependencies

- **Slice 01** - the selector must exist for the named trend to be reachable or demoable.
- Epic 5251's `GetNamedCycleTimePercentilesForTeam` (+ Portfolio twin) - shipped.

## Effort estimate / reference class

~1 day. Reference class: Epic 5251's own `cycleTimePercentiles` `definitionId` extension - the same
controller idiom, the same service duplication, plus this slice's cache-key segmentation and the FE
trend wiring.

## Pre-slice SPIKE

None. The shape is known, the named computation is shipped, and the one trap (cache key) is already
identified with a fix to mirror.

## Taste tests

- **Value-bearing**: Priya answers "did last quarter's fix to the validation queue actually work?" -
  a question no existing surface answers. PASS.
- **Not 4+ new components**: 0 new components; 4 modified (2 controllers, 2 services) + FE wiring. PASS.
- **Abstraction-first**: no new abstraction - reuses the shipped named computation and the existing
  Info DTO. PASS.
- **Disproves a pre-commitment**: yes - that the Info endpoint shape generalises to named windows. PASS.
- **Production data, not synthetic**: AC are demo-data-driven, and the collision AC is a real
  concurrency-shaped failure, not a plumbing check. PASS.
- **Dogfood same day**: the trend footer is on the landing page; visible the moment it ships. PASS.
- **Not identical-except-scale to slice 01**: different stack (BE+FE vs FE-only), different surface. PASS.
