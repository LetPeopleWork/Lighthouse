# RCA — Bug #5857: Cycle Time / Work Item Age blank in the Work Item dialog

**Status:** root cause confirmed, fix direction approved by maintainer 2026-08-27.

## Symptom

The shared Work Item dialog renders Cycle Time and Work Item Age as empty cells when opened
from the Throughput Run Chart, the Arrivals Run Chart, and Work In Progress over Time — and
from the "View Data" table. It renders correctly when opened from the Cycle Time scatter chart.

## Root cause

`WorkItemBase` is simultaneously the EF entity and the wire contract for seven metrics
endpoints. Commit `8c00e49b2` (2026-07-27, Bug #5567) converted `CycleTime` and `WorkItemAge`
from get-only properties into methods taking a time zone and a calendar day. That change was
correct for its own purpose — a property cannot take a parameter — but System.Text.Json cannot
serialise a method, so both fields silently disappeared from every response returning the raw
entity. First release carrying the regression: v26.8.1.14.

Chain, with evidence:

1. `WorkItemsDialog.tsx:213` renders whatever `valueGetter` returns, unguarded. `undefined`
   renders as an empty cell; `0` would render "0".
2. Affected callers read the field straight off the item, sourced from
   `RunChartData.workItemsPerUnitOfTime`, which `MetricsService.ts:326` passes through verbatim
   with no mapping and no validation.
3. Those endpoints return `ActionResult<RunChartData>`, and `RunChartData.cs:16` holds
   `Dictionary<int, List<WorkItemBase>>` — the persistence entity. The path that works returns
   `WorkItemDto` (`TeamMetricsController.cs:260`).
4. `WorkItemBase.cs:62` `public int CycleTime(TimeZoneInfo zone)` and `:86`
   `public int WorkItemAge(TimeZoneInfo zone, DateOnly today)` are methods, so STJ emits neither.
5. Root: the entity doubles as the wire format, and no test, type or guard was positioned to
   notice two fields leaving seven responses.

Verified empirically, not inferred: a scratch console app compiled against the built
`Lighthouse.dll` serialised a real `RunChartData` with the production converter set. The emitted
key set is `id, referenceId, parentReferenceId, name, type, state, tags, stateCategory, url,
order, createdDate, startedDate, closedDate, currentStateEnteredAt, lastChangedRemote,
additionalFieldValues, syncedTransitions` — no `cycleTime`, no `workItemAge`, no `isBlocked`.

## Blast radius beyond the report

- Cycle Time PBC drill-through, the other PBCs, and Estimation vs Cycle Time are also broken,
  unreported. They resolve items through `workItemLookup`, which `BaseMetricsView.tsx:186-195`
  seeds from run-chart items with an unconditional `set` and adds DTO-backed sources only
  `if (!has)` — so the impoverished record wins.
- `isBlocked` is absent from every affected dialog, so the blocked marker never draws.
- On portfolios, `size` and `owningTeam` are absent too: `List<WorkItemBase>` serialises by
  declared type, flattening `Feature` to its base. `hasOwningTeams` (`WorkItemsDialog.tsx:81`)
  is therefore false and the "Owned by" column silently disappears.
- CSV export carries the blank column, because `DataGridBase` exports the rendered grid.
- 11 of the 25 "View Data" payloads are affected.
- `PortfolioMetricsController.cs:55` (`/started`) has no frontend consumer — a dead endpoint
  still leaking the shape.

Run charts themselves are unaffected; they plot bucket counts.

## Refuted hypotheses

- The Bug #5571 category-scoped fetch gate. `categoryMetadata.ts:281-286` declares the required
  sources and the payloads are fetched — the bars render.
- Truncation, a second fetch, or a JSON reviver. One GET, straight into the constructor; the
  only axios interceptor is an HTML-response guard.
- Enum-as-string / case-insensitivity. `stateCategory` is emitted as `"Done"` and consumed as a
  string union — correct on both ends.

## Approved fix

Map the run chart to a DTO at the boundary. New `RunChartDataDto` holding
`Dictionary<int, List<RunChartWorkItemDto>>`, where `RunChartWorkItemDto : WorkItemDto` adds
`Tags` and `AdditionalFieldValues`.

Those two extra fields are load-bearing. `evaluateCondition.ts:3-11` declares them as the
rule-evaluable surface for the premium client-side throughput filter, and
`ForecastFilterThroughputChartIntegrationTest.cs:222` pins that the payload must carry
rule-evaluable data. `evaluateConditions` has no production caller today, so a plain
`WorkItemDto` swap would go green and quietly demolish that contract later.

Change the return type at all seven sites. `team`/`portfolio`, `clock` and `blockedItemService`
are already in scope at each one, so `isBlocked` comes back for free.

**Age anchoring (maintainer decision, 2026-08-27):** `wipOverTime` passes `asOf = endDate`,
matching `/wip` (`TeamMetricsController.cs:164`) and the aging chart's assumption. `throughput`
and `arrivals` stay today-anchored. Per-bucket ages are a separate question, deliberately not
opened here.

Secondary, defence in depth: reorder `buildWorkItemLookup` (`BaseMetricsView.tsx:186-195`) to
seed from `cycleTimeData` and `inProgressItems` first and add run-chart items as fill-ins. Inert
once the DTO lands, but it removes an accidental precedence.

Rejected: patching each widget's `valueGetter`. It leaves View Data, `isBlocked` and
`owningTeam` broken and grows the surface that must be re-fixed next time.

## Files

Change:

- `Lighthouse.Backend/Lighthouse.Backend/API/DTO/RunChartDataDto.cs` (new)
- `Lighthouse.Backend/Lighthouse.Backend/API/TeamMetricsController.cs` — lines 54, 80, 96
- `Lighthouse.Backend/Lighthouse.Backend/API/PortfolioMetricsController.cs` — lines 39, 55, 71, 87
- `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx` — lines 186-195

Read only, do not touch: `Models/WorkItemBase.cs` (the methods are correct; the wire format is
what is wrong), `API/DTO/WorkItemDto.cs`, `Models/Metrics/RunChartData.cs`.

Verify unchanged after the fix: `BarRunChart.tsx:157`, `LineRunChart.tsx:205`,
`ProcessBehaviourChart.tsx:195-216`, `EstimationVsCycleTimeChart.tsx:294`,
`WorkItemsDialog.tsx:191-213` — all become correct without edits.

## Risks

| Risk | Severity | Handling |
|---|---|---|
| Dropping `tags` / `additionalFieldValues` breaks the forecast filter | High | Carried on the DTO by design; `ForecastFilterThroughputChartIntegrationTest` is the canary — verify it stays green |
| `WorkItemAge` anchoring changes what the WIP dialog reports | Medium | Decided above: `asOf = endDate` for `wipOverTime` |
| `IBlockedItemService.IsBlocked` now runs per item per day-bucket | Medium | Measure on a wide date range; memoise per item id within the request if hot |
| Payload size | Low | Roughly neutral: +4 computed fields, −`order`/`lastChangedRemote`/`syncedTransitions` |
| Portfolio "Owned by" column reappears | Low–Med | This is the fix working, but it moves pixels — re-shoot affected `@screenshot` runs (`rm` the PNG first; import the gitignored premium licence fixture) |
| Existing frontend tests break | Low | They mock the dialog and hand-build fixtures, so they are insulated — which is itself the problem |
| Migration / data | None | Serialisation only. No schema change, no migration |

## Regression test

Primary, and the only test that would have failed on `8c00e49b2`: a backend NUnit integration
test at `Lighthouse.Backend.Tests/API/Integration/RunChartPayloadContractIntegrationTest.cs`,
following the existing `*ReadApiIntegrationTest.cs` convention. Seed one closed item with known
start and close dates plus one in-progress item, then parametrise over all seven endpoints and
parse the body with `JsonDocument`:

1. `cycleTime` is present. This is the assertion that fails today.
2. `workItemAge` is present.
3. `isBlocked` is present.
4. Values are correct, not merely present — the closed item's `cycleTime` equals the inclusive
   day span in the instance zone, and the in-progress item's `workItemAge` matches. Presence
   alone would survive a mapper that hardcodes `0`.
5. Cross-endpoint agreement: an item appearing in both `/throughput` and `/cycleTimeData`
   reports the same `cycleTime`. Two paths to the same item must not disagree.
6. `tags` and `additionalFieldValues` are still present.
7. Portfolio endpoints only: `size` and `owningTeam` are present, guarding the declared-type
   flattening.

Reuse `RunChartDataGenerator` for seeding.

Secondary, frontend Vitest: given the same item id in both `throughputData` and `cycleTimeData`,
`buildWorkItemLookup` returns the record carrying `cycleTime`.

Not the place: `BarRunChart.test.tsx` / `LineRunChart.test.tsx`. They mock the dialog and build
their own fixtures, so strengthening them tests the fixture, not the product.

## Prevention (follow-up, not part of this fix)

- Add a zod `WorkItemSchema` and run the run-chart fetchers through `BaseApiService.parse`. The
  mechanism already exists and already covers `Feature`. Feeds US #5232.
- ArchUnit rule: no controller action may expose a type reachable from the `DbSet`s on
  `LighthouseAppContext`. Model it on `CalendarDayAnchorSeamArchUnitTest`'s reflection approach —
  it would have failed on the regressing commit.
- `docs/ci-learnings.md`: converting an entity property to a method removes a JSON field from
  every endpoint returning that entity, and STJ serialises by declared type, so subclass fields
  vanish inside a `List<Base>`.
- Make `IWorkItem.cycleTime` / `workItemAge` optional in TypeScript so `tsc` forces every
  `valueGetter` to handle absence.
