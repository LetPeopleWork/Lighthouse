# Slice 03 — Work Item Age as of the last day of the selected range

**Story**: 5508 Cleanup Widget lose Ends | **Group**: C (data) | **Job**: `job-flow-coach-see-age-as-of-selected-date`

## Goal (one sentence)
Make every Work-Item-Age surface report the age of the population that was in progress on the last day of the selected range, aged to that day — so selecting a past period tells the truth about that period instead of about today.

### Elevator Pitch
Before: select last month and the Work Item Age Percentiles card ages every item to today and silently omits everything that has closed since.
After: open **Team → Metrics**, set the range to a past month → the **Work Item Age Percentiles** card, the **Total Work Item Age** widget and the **Work Item Aging** chart all report ages as they stood on the last day of that range.
Decision enabled: review a past period honestly in a retro — "our WIP was ageing to 30 days back then" — instead of reading today's numbers under last month's heading.

### The defect being fixed
`WorkItemBase.WorkItemAge` (`Models/WorkItemBase.cs:71`) computes `GetDateDifference(referencedDate, DateTime.UtcNow)` and returns `0` unless `StateCategory == Doing` *right now*. So `GetWorkItemAgePercentilesForTeam(team, endDate)` (`TeamMetricsService.cs:319`) takes the correct WIP snapshot as of `endDate` and then ages every item to today, while the `age > 0` filter drops everything that has since closed.

The arithmetic already exists and is already trusted: `BaseMetricsService.GenerateTotalWorkItemAgeByDay` (`BaseMetricsService.cs:808`) selects `items.Where(i => WasItemProgressOnDay(currentDate, i))` and computes `age = (currentDate − (StartedDate ?? CreatedDate)) + 1`.

### Domain examples (item started 01 July, closed 06 July; today = 18 July)
1. Range ends 04 July → item included, age 4.
2. Range ends 06 July → item included, age 6.
3. Range ends 10 July → item excluded (closed before the day).
4. Range ends today → identical to current behaviour for still-open items (regression guard).
5. Item started after the range end → excluded, not aged to 1.

### Outcome KPI
Work Item Age historical accuracy: 0 divergence between the WIA percentile population/ages and the as-of-date reference computation for any selected range. Plus the live-view regression KPI: 0 changed values when `endDate` = today.

## IN scope
- A backend as-of-date age computation, applied to: the Work Item Age Percentiles read (`GetWorkItemAgePercentilesFor{Team,Portfolio}`), the Total Work Item Age overview value, and the item ages the Work Item Aging chart plots (`WorkItemAgingChart.tsx:212` reads `item.workItemAge` from the DTO).
- Population selection by `WasItemProgressOnDay(endDate, item)` rather than current `StateCategory`.
- Reuse of the `GenerateTotalWorkItemAgeByDay` arithmetic as the single source of truth for "age on a day" — extract it rather than reimplementing it (D4).
- Backend tests pinning the new computation against that reference, plus the `endDate = today` regression assertion.
- A write-back regression test proving emitted write-back age values are unchanged (CI5).
- Team and Portfolio scope.

## OUT of scope
- **Changing the `WorkItemAge` property itself.** It is consumed by `WriteBackValueSource.cs`; repointing it at an arbitrary date would change what Lighthouse writes into Jira/ADO. DESIGN picks the mechanism (an `AgeOnDay(date)` helper is the obvious candidate) under the hard constraint that write-back semantics stay put.
- Any new persisted snapshot table or EF migration — this computes from existing started/closed dates.
- RAG or trend on the percentiles widget (slice 04, which depends on this).
- Age-in-state / pace-band percentiles (`aging-pace-percentiles`), a different population.

## Learning hypothesis
- **Disproves if it fails**: that as-of-date age is derivable from data already on hand. If `WasItemProgressOnDay` cannot reconstruct the historical population — e.g. items lack a reliable `StartedDate`, or state transitions are not recoverable far enough back — then honest historical age needs persisted snapshots, and slice 04's previous-period trend is not affordable in this story.
- **Confirms if it succeeds**: every Work-Item-Age surface can be made date-honest with no new persistence, and the previous-period trend in slice 04 is a cheap follow-on.

## Acceptance criteria
1. Given an item started day D and closed day D+5, when the range ends day D+3, the item is in the Work Item Age population with age 4 — neither aged to today nor dropped for being closed now.
2. Given the range ends today, every Work-Item-Age number is identical to pre-change behaviour.
3. The Work Item Aging chart's dot heights use the same as-of-`endDate` age as the percentile card — the two surfaces never disagree for the same range.
4. The Total Work Item Age overview widget reports the as-of-`endDate` value.
5. Write-back emitted age values are unchanged (regression test).
6. The computation is asserted against `GenerateTotalWorkItemAgeByDay` at the same date, so the two cannot drift.
7. Holds at Portfolio scope as well as Team scope.
8. E2E (demo data): a demo team with a historical range shows non-zero WIA percentiles including at least one item that has since closed.

## Dependencies
None inbound. **Slice 04 depends on this** — the previous-period trend (D5) is this same computation evaluated at `startDate − 1 day`.

## Effort / reference class
**~0.5 day (revised down by DESIGN, 2026-07-18 — was ~1 day).** DESIGN established that the WIP-snapshot population is *already* date-correct at both `GetWipSnapshotForTeam` and `GetInProgressFeaturesForPortfolio` (both filter through `WasItemProgressOnDay`), and that `GetTotalWorkItemAge` is *already* correct too. The slice reduces to: one new `WorkItemBase.AgeOnDay(DateTime)` method, two `.Select(...)` projection changes (`TeamMetricsService.cs:326`, `PortfolioMetricsService.cs:284`), and one optional `asOf` parameter on `WorkItemDto` consumed only by the `/wip` endpoint. US-04 AC4 (Total Work Item Age) becomes a regression assertion rather than new work. See DESIGN decisions D13-D17 for the full mechanism and constraints. Reference class: the `GenerateTotalWorkItemAgeByDay` / `totalWorkItemAgeOverTime` work, which already established the by-day arithmetic this slice reuses.

## Pre-slice SPIKE
**Recommended, timeboxed ~1h**: on demo data, run `WasItemProgressOnDay(pastDate, item)` over a historical range and confirm the reconstructed population matches what the over-time chart already renders for that day. This is the cheapest possible test of the learning hypothesis and it gates the rest of group C. Demo-data adequacy is already confirmed (2026-07-18): `DemoDataFactory.ReplaceDatePlaceholders` resolves `{w-N}` to N business days before today at seed time, and Team Zenith carries 201 closed items spanning ~`{w-145}` to recent — AC8 is satisfiable with no demo-data extension, and ranges can be selected deterministically because seeding is relative to today.
