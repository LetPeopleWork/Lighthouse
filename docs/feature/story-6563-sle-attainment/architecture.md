# Architecture brief: story\-6563\-sle\-attainment

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Paradigm object_oriented: the backend is OO ports-and-adapters (CLAUDE.md); domain rules live on value objects in Modules/FlowMetrics/Domain, application services coordinate ports, frontend keeps its hook-and-pure-function style already used by ragRules*.ts.
```

#### D2

```text
Where the share is computed (Open, Design -> decided): the backend owns the share for both surfaces. New value object WithinSleShare (Modules/FlowMetrics/Domain/WithinSleShare.cs) is a closed choice NoSle | TooFew(closed) | Measured(within, closed, sle), built only by WithinSleShare.Of(cycleTimesInDays, sle). It is the one owner of the rule a Work Item is within when its Cycle Time is at or below the SLE days, and of the minimum of 5 (MinimumClosed). New value object ServiceLevelExpectation (Domain/ServiceLevelExpectation.cs; Percentile 1..100, Days >= 1) with For(WorkTrackingSystemOptionsOwner) returning null when either setting is 0 or less. The widget info and every daily record row go through WithinSleShare; the frontend never recounts.
```

#### D3

```text
S07b #6378 (BaseMetricsService to composition) is NOT a Before item: the share is not added to BaseMetricsService. New application services read closed cycle times through the existing ITeamMetricsService.GetCycleTimeDataForTeam and IPortfolioMetricsService.GetCycleTimeDataForPortfolio (default Cycle Time, cache and filters as the sibling widgets), so the base class gains no capability S07b would have to move.
```

#### D4

```text
Residual duplicate kept on purpose: calculateSLEStats in Lighthouse.Frontend/src/pages/Common/MetricsView/ragRulesTimeAndAge.ts still counts within-SLE for the Cycle Time Percentiles status, because the scope excludes any change to that widget and its status (Q7). Follow-up for the improvement backlog: move that status onto the backend share.
```

#### D5

```text
Read ports follow the S12a #6386 target shape, one query port per capability: IWithinSleInfoQuery (widget) and IWithinSleOverTimeSeriesQuery (chart) in Modules/FlowMetrics/Application, injected into TeamMetricsController and PortfolioMetricsController next to IPercentilesOverTimeSeriesQuery.
```

#### D6

```text
Widget read (S5): new GET teams/{id}/metrics/withinSleInfo and portfolios/{id}/metrics/withinSleInfo with startDate, endDate (inclusive days, inverted window -> 400 with the existing message). Answer WithinSleInfoDto(Verdict NoSle|TooFew|Measured, ClosedCount, WithinCount, SlePercentile?, SleDays?, SharePercent?, Comparison InfoWidgetComparisonDto) in its own file Models/Metrics/WithinSleInfoDto.cs; InfoWidgetDtos.cs is not touched.
```

#### D7

```text
Header trend (S6, Q2 previous-period): the comparison measures the preceding period of the same length exactly as cycleTimePercentilesInfo does (previousEnd = start - 1 day, previousStart = start - periodDays). Direction up/down by the sign of the share difference rounded to 0.1 points, flat at 0.0; delta text like +4.1 pts as sketched. The Proposed rule no comparison when either period is not Measured is adopted (direction none), because a share that is not judged (Q5) cannot be compared; it still awaits Product confirmation.
```

#### D8

```text
Status (S6, Q7) stays a frontend RAG rule like all sibling statuses (Signals is target only): extract statusAgainstTarget(sharePercent, targetPercent) from computeCycleTimePercentilesRag as the one owner of Sustain / Observe up to 20 points below / Act beyond, and add computeWithinSleRag(info, terms) that maps NoSle to Act and TooFew to no status. Cycle Time Percentiles output stays byte-identical.
```

#### D9

```text
Widget placement (Open, Design -> decided): widgetKey withinSle, size small, in flow-overview directly before percentiles (after predictabilityScore), no ownerFilter (Teams and Portfolios, Q6), trendPolicies withinSle previous-period, widgetFetchRequirements withinSle [withinSleInfo, cycleTimeData] because View Data reuses the Cycle Time Percentiles closed-items dialog fed by cycleTimeData. Title from terminology: Within {SLE}.
```

#### D10

```text
Loading and could-not-load looks (Open, Design -> decided): the widget takes WidgetShell status from useMetricsData fetchStates of the new fetch key withinSleInfo (useMetricsQuery, keyed by owner and selected window); the chart owns its fetch through a keyed TanStack query [owner id, ownerUpdatedAt, horizon, from, to] mapped to WidgetShell status by seriesStatusOf, so a late answer for an old horizon or range never lands and the previous answer is never shown as current.
```

#### D11

```text
Daily record (S7): new entity WithinSleSnapshot (Models/WithinSleSnapshot.cs, entities still sit in Models per ADR-270): Id, OwnerId, OwnerType, RecordedAt (DateOnly, named like the other snapshot tables), Horizon (30|60|90), ClosedCount, WithinCount, SlePercentile, SleDays, Origin (WithinSleSnapshotOrigin Recorded=0 | FilledIn=1). Unique index (OwnerId, OwnerType, Horizon, RecordedAt). Counts are stored, never a percentage; the share and verdict are derived on read by WithinSleShare. A row is written whenever an SLE is set, for any closed count including 0, so the tooltip can state the count; no row is written while no SLE is set.
```

#### D12

```text
Writer (S7): IWithinSleSnapshotWriter / WithinSleSnapshotWriter in Application, the single writer of WithinSleSnapshot rows, shaped like IPercentileSnapshotWriter: RecordToday overwrites today (row takes the SLE in force at the last recording of the day and Origin Recorded), FillDayIfAbsent never touches an existing row and writes Origin FilledIn against the SLE current at fill time (Q4), SaveFilledDay uses LostRaceTolerantSave. Family reader record WithinSleFamilyReader(ReadClosedCycleTimes(windowStart, windowEnd), CurrentSle()) in its own file.
```

#### D13

```text
Refresh events that record the day (Open, Design -> decided): TeamDataRefreshed and PortfolioFeaturesRefreshed, the same events as the percentile snapshots, through a new WithinSleRecordingHandler in Application/Policies with its own failure containment and alert family WithinSle. Registered in Program.cs beside ProcessBehaviorRecordingHandler, not in FlowMetricsModule: AddFlowMetrics runs before the handler block, so module registration would place it before TeamDataRefreshedTeamMetricsUpdateHandler and record stale metrics; DomainEventHandlerOrderPinTest pins the order. Cost accepted: the Program.cs edit runs the full backend Integration suite in S7.
```

#### D14

```text
Chart read (S7, Open -> decided): a new endpoint, not an extension of percentiles-over-time (different row shape; that endpoint already has external client consumers). GET teams/{id}/metrics/within-sle-over-time and portfolios/{id}/... with horizon 30|60|90 (anything else -> 400 naming the allowed values), optional startDate/endDate inclusive. Answer WithinSleOverTimeDto(CurrentSlePercentile?, CurrentSleDays?, Days WithinSleDayDto[]), each day RecordedAt, Horizon, ClosedCount, WithinCount, SlePercentile, SleDays, Origin, Verdict, SharePercent? so the frontend draws a point only where Verdict is Measured and never re-decides the minimum.
```

#### D15

```text
Fill-in (S8): OverTimeFillTarget gains WithinSleFamilies built beside the percentile families and guarded by a new RetentionEdge.Guard(WithinSleFamilyReader) overload; OverTimeHistoryFiller.FillOneDayAsync stages and saves the within-SLE family as a third TryWriteDayAsync, so one pass fills every chart of the owner. GapAskingWithinSleOverTimeSeriesQuery wraps the stored read and asks IOverTimeGapReconciler for missing days, registered in FlowMetricsModule like the percentile wrapper. No demo backfill handler: past days come from the filler when the chart opens.
```

#### D16

```text
Formal verification: claim each recorded day keeps its share and SLE; a fill never changes an existing day; a later row of a past day is always FilledIn; within never exceeds closed; no point below 5 -> Quint (state model over record, fill, SLE change and day advance), docs/feature/story-6563-sle-attainment/design/withinSleDailyRecord.qnt, checked by quint run per invariant and quint verify (Apalache, 6 steps). TLA+/TLC not needed: no fairness or temporal claim. Agda not needed: no type-level proof. Concurrency of two app copies is not modelled: the unique index plus LostRaceTolerantSave covers it and is observed by a SQLite service-port test like TwoCopiesFillingTheSameDayAtThePortTest. The share arithmetic (WithinSleShare.Of) is a pure function law checked by a default-suite property test, not by the model.
```

#### D17

```text
No liveness claim: no product promise says a missing day is eventually filled; filling is bounded per chart load (90 days, 10 s) and resumes on the next load, so progress depends on readers opening the chart, which no fairness assumption can honestly cover.
```

#### D18

```text
Second stressor walkthrough (not executed, design reading): blackout periods and the forecast filter reach the share through the same cycle-time population as Cycle Time Percentiles, so both widgets agree; a named Cycle Time selected on Cycle Time Percentiles does not change Within SLE (default Cycle Time only, scope); a demo load firing a refresh on partial data rewrites only today, which the next refresh corrects; a Portfolio with no closed Deliveries gives TooFew(0), not NoSle; the RetentionEdge guard makes a day reaching past retained items read empty so it is not filled as a false 0 share.
```

#### D19

```text
Usage-data event: not decided here; asked in the DEVOPS wave (Open, owner Product).
```

#### D20

```text
Context map: Flow metrics only. New glossary terms for ADR-270 Flow metrics: share within the SLE (of the Work Items closed in a window, the part whose Cycle Time is at or below the SLE days) and SLE in force on a day (the SLE stored with each daily record row). New concept: the Within SLE daily record (a fourth day-keyed snapshot table). No domain event published; reacts to TeamDataRefreshed and PortfolioFeaturesRefreshed. UI surfaces: the unit Metrics tab, Flow Overview and Predictability, unchanged. ADR-270 and ARCHITECTURE.md section 4 are updated in S7, where the table lands (Proposed -> confirmed).
```

#### D21

```text
Slice split confirmed as proposed: S5 is the walking skeleton (real closed Work Items and the owner real SLE through the running app); S6 carries the four verdict states; S7 carries the chart own too-few, no-SLE and no-history states; S8 the fill-in. Named Cycle Times out (confirmed). Between S5 and S6 nothing is pushed (AFK: nothing pushed until the maintainer says so), so the S5 body for NoSle/TooFew is never released alone.
```

### Constraints

#### K1

```text
Minimum closed Work Items for a verdict or a chart point: 5 (Q5); fewer gives TooFew with the count. Bound applies to the selected range (widget) and to each day trailing window (chart).
```

#### K2

```text
Status thresholds (Q7): Sustain when share >= target; Observe when below by up to and including 20 points; Act when more than 20 points below or no SLE set (Q10).
```

#### K3

```text
Horizons: exactly 30, 60, 90 trailing days (Q3); no other horizon is stored or served.
```

#### K4

```text
SLE inputs as validated today: probability (percentile) and range (days) are integers; an SLE exists only when both are greater than 0. No upper bound on days is introduced; percentile is 1..100 as the settings already accept. No narrower or wider bound is designed.
```

#### K5

```text
Date range: no maximum length is imposed on the widget range or the chart window (as siblings); the chart fill is bounded by the existing filler budget, 90 days and 10 s per pass.
```

#### K6

```text
Counts: 0 <= WithinCount <= ClosedCount; no upper bound on ClosedCount.
```

#### K7

```text
Free (Q8): no licence check on either endpoint or surface.
```

#### K8

```text
EF migration additive only, generated with the CreateMigration PowerShell script for every provider; RecordedAt keys the day.
```

#### K9

```text
Backend acceptance scenarios drive the application-service ports (IWithinSleInfoQuery, IWithinSleSnapshotWriter, IWithinSleOverTimeSeriesQuery) through ServicePortHarness; no test host except a named skeleton or wire contract (TestHostUseGuardTest). Test-writing skill rules override nWave timing and PBT density.
```

#### K10

```text
Configurable terms (SLE, Work Item, Cycle Time) render through useTerminology; no customer or informant name anywhere.
```

#### K11

```text
Out of scope and unchanged: Cycle Time Percentiles widget and status, SLE Risk, status tooltips wording, Aging chart SLE line, Then & Now reports, Lighthouse-Clients, Named Cycle Times.
```

#### K12

```text
Q1: the widget counts the Work Items closed in the selected date range, like its sibling widgets; filters apply.
```

#### K13

```text
Q2: the change against the preceding period lives in the widget's header trend indicator (previous-period policy), not in the widget body.
```

#### K14

```text
Q3: the daily record has trailing 30, 60 and 90 day horizons with a toggle, like Percentiles Over Time.
```

#### K15

```text
Q5: fewer than 5 closed Work Items gives no verdict on the widget and no point on the chart.
```

#### K16

```text
Q6: both the widget and the chart appear for Teams and Portfolios.
```

#### K17

```text
Q7: the widget reuses the Cycle Time Percentiles status rule (Sustain at or above target, Observe up to 20 points below, Act beyond or no SLE); Cycle Time Percentiles keeps its own status.
```

#### K18

```text
Q8: free, no Premium gate.
```

#### K19

```text
Q9: the tooltip wording and the Aging chart line default are out of this Story.
```

#### K20

```text
Q10: with no SLE set the widget says so and its status is Act.
```

#### K21

```text
Q11: Lighthouse-Clients follow in a later Story.
```

#### K22

```text
Q4: answer 'Record SLE per day', whose option read: 'Each recorded day stores the SLE in force that day, so changing the SLE does not rewrite history. Filled-in past days (before recording) use today's SLE and the chart says so.'
```

#### K23

```text
Q12: answer 'Decide at finalize' - a deferral: whether the website gets a line is decided at finalize.
```

#### K24

```text
Sketch review: the widget is titled 'Within SLE'; View Data reuses the Cycle Time Percentiles closed-items dialog; View Data, the trend and the status sit in the WidgetShell header, not in the widget body.
```

#### K25

```text
Sketch review: the chart has a fixed 0-100 axis 'Within SLE (%)', one accent line, a dashed grey target line labelled at its right end that steps on the day the SLE changed, the 30/60/90 day toggle, and a tooltip with day, share, count, horizon and target.
```

#### K26

```text
Scope: no SLE panel in a Then & Now report, and no change to Cycle Time Percentiles' status - from the maintainer's DIVERGE scope choice 'Mix + daily record' and the Q7 option 'Cycle Time Percentiles keeps its own status unchanged'.
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

None.

### Boundaries

```text
HTTP GET withinSleInfo and within-sle-over-time on TeamMetricsController / PortfolioMetricsController, delegating to the application ports IWithinSleInfoQuery and IWithinSleOverTimeSeriesQuery; domain events TeamDataRefreshed / PortfolioFeaturesRefreshed driving WithinSleRecordingHandler; chart loads driving the OverTimeHistoryFiller through IOverTimeGapReconciler
```

#### Driven ports

```text
ITeamMetricsService / IPortfolioMetricsService (closed cycle-time population, existing)
```

```text
IWithinSleSnapshotRepository (new, persistence of WithinSleSnapshot through LighthouseAppContext)
```

```text
ILighthouseClock (existing, the instance day)
```

```text
IRepository<Team> / IRepository<Portfolio> (existing, the owner and its SLE settings)
```

```text
API controllers -> FlowMetrics Application ports -> FlowMetrics Domain (WithinSleShare, ServiceLevelExpectation are pure, no I/O); Adapters/Persistence and Adapters/Background implement or drive Application ports; Domain depends on nothing in Application or Adapters; frontend widgets -> MetricsService -> HTTP only
```

#### Failures

##### B1

```text
horizon not 30, 60 or 90 on within-sle-over-time
```

Refusal

```text
400 with a message naming the allowed horizons
```

##### B2

```text
startDate after endDate on either endpoint
```

Refusal

```text
400 with the existing start-before-end message
```

##### B3

```text
owner id unknown
```

Refusal

```text
404 via GetEntityByIdAnExecuteAction
```

##### B4

```text
recording throws on a refresh
```

Indeterminate

```text
error logged with family WithinSle; no row that day; other families unaffected; next refresh records again
```

##### B5

```text
a fill day cannot be written
```

Retry

```text
logged once per pass; not memoised; the next chart load asks again
```

##### B6

```text
frontend request fails
```

Indeterminate

```text
WidgetShell could-not-load look; no previous answer shown as current
```

### Observations

```text
Widget answer WithinSleInfoDto: Verdict, ClosedCount, WithinCount, SharePercent, SLE target and the previous-period Comparison for the selected range
```

WithinSleInfoDto

```text
which Work Items are within or late (the View Data dialog shows them); Named Cycle Times; whether the SLE changed inside the range (only the current SLE is applied)
```

```text
Chart answer WithinSleOverTimeDto: current SLE and, per stored day, counts, SLE in force, Origin, Verdict and SharePercent
```

WithinSleOverTimeDto

```text
a day never attempted from a day outside the stored history or one recorded while no SLE was set (all are absent days); which SLE was set on a filled-in day before recording started (Q4)
```

```text
Rendered widget and chart: large share with count, sentence, target, status colour and word, header trend; chart points, open versus filled markers, stepped target line, notes
```

DOM \(RTL \/ Playwright\)

```text
how the share was computed; stale cache versus fresh value within the same owner update
```

### Equality

WithinSleDayDto observation

```text
Two stored days are equal when OwnerId, OwnerType, Horizon, RecordedAt, ClosedCount, WithinCount, SlePercentile, SleDays and Origin agree; Id is not observed
```

WithinSleShare observation

```text
Two shares are equal when Verdict, ClosedCount, WithinCount and the SLE (percentile, days) agree; the order of the input cycle times is not observed
```

### Constructors

WithinSleShare\.Of

static WithinSleShare Of\(IReadOnlyList\<int\> cycleTimesInDays\, ServiceLevelExpectation\? sle\)

ServiceLevelExpectation\.For

static ServiceLevelExpectation\? For\(WorkTrackingSystemOptionsOwner owner\)

WithinSleSnapshotWriter\.RecordToday

void RecordToday\(int ownerId\, OwnerType ownerType\, WithinSleFamilyReader family\)

WithinSleSnapshotWriter\.FillDayIfAbsent

void FillDayIfAbsent\(int ownerId\, OwnerType ownerType\, DateOnly day\, WithinSleFamilyReader family\)

### Laws

#### L1

```text
For every list of cycle times and every SLE or none, WithinSleShare.Of gives NoSle when the SLE is absent; TooFew(n) when n < 5; otherwise Measured with within = count of cycle times <= SLE days, 0 <= within <= closed and SharePercent = within / closed * 100; the result does not depend on input order
```

Invariant

Domain

WithinSleShare

##### Treatment

Broad

###### Generator

```text
cycle times 0..400 days, list length 0..40, SLE absent or percentile 1..100 with days 1..60
```

list length 40

fixed seed per run printed on failure\; NUnit seeded Random generator \(no PBT library in the backend test project\)

###### Must include

```text
empty list
```

```text
exactly 4 and exactly 5 cycle times
```

```text
a cycle time equal to the SLE days
```

```text
no SLE
```

flat list

```text
manual: on failure retry with the list halved and each element moved toward 0, report the smallest failing case
```

```text
oracle counts by a direct loop over the list and compares with the verdict fields
```

```text
counting < instead of <=, a minimum of 4 or 6, or a share computed over all Work Items rather than closed ones
```

##### Evidence

None.

#### L2

```text
statusAgainstTarget(share, target) is Sustain when share >= target, Observe when 0 < target - share <= 20, Act when target - share > 20; computeWithinSleRag maps NoSle to Act and TooFew to no status
```

Invariant

Domain

ragRulesTimeAndAge\.statusAgainstTarget

##### Treatment

Finite

###### Cases

```text
share 70 target 70 -> Sustain
```

```text
share 69.9 target 70 -> Observe
```

```text
share 50 target 70 -> Observe
```

```text
share 49.9 target 70 -> Act
```

```text
NoSle -> Act
```

```text
TooFew(3) -> no status
```

```text
closed set of boundary cases of a threshold rule
```

##### Evidence

None.

#### L3

```text
A day that ended with a row keeps that row (counts, SLE, origin) under every later record, fill and SLE change; a later row for a past day is FilledIn; RecordToday overwrites only today
```

Invariant

Application

WithinSleSnapshotWriter

##### Treatment

Finite

###### Cases

```text
record day 1 at SLE 70/10, change SLE to 85/14, advance and record day 2 -> day 1 still 70/10 Recorded
```

```text
fill a day already recorded -> row unchanged
```

```text
fill an absent past day -> FilledIn against the current SLE
```

```text
fill today then record today -> Recorded
```

```text
no SLE set -> no row written
```

```text
day with 3 closed -> row stored, Verdict TooFew, no point
```

```text
the state space is covered by the Quint model; these examples are its executable projection on the pure writer with an in-memory repository
```

##### Evidence

docs\/feature\/story\-6563\-sle\-attainment\/design\/withinSleDailyRecord\.qnt

#### L4

```text
Two copies filling the same owner, horizon and day leave one row: the unique index refuses the second insert and SaveFilledDay drops it
```

Invariant

Adapter

WithinSleSnapshotWriter\.SaveFilledDay

##### Treatment

Finite

###### Cases

```text
two scopes fill the same day on one SQLite file -> one row, first copy values
```

```text
only a real database enforces the unique index; one race case at the service port
```

##### Evidence

None.

#### L5

```text
A WithinSleSnapshot staged by the writer and read back through IWithinSleOverTimeSeriesQuery is equal under the WithinSleDayDto observation
```

RoundTrip

Adapter

WithinSleSnapshotRepository

##### Treatment

Finite

###### Cases

```text
Recorded row 8/11 at 70/10 horizon 30
```

```text
FilledIn row 0/0 at 85/14 horizon 90
```

```text
finite field mapping; both origins and a zero-count row
```

##### Evidence

None.

#### L6

```text
The header comparison is none when the current or the previous period is not Measured; otherwise direction is the sign of the share difference rounded to 0.1 points
```

Invariant

Application

WithinSleInfoQuery

##### Treatment

Finite

###### Cases

```text
current 72.7, previous 68.6 -> up +4.1 pts
```

```text
equal shares -> flat
```

```text
previous TooFew -> none
```

```text
current NoSle -> none
```

```text
closed set of comparison cases
```

##### Evidence

None.

#### L7

```text
The writer and handler refine the Quint model: RecordToday = recordToday, FillDayIfAbsent = fillDayIfAbsent, an SLE settings save = changeSle, ILighthouseClock.Today moving = advanceDay
```

Refinement

Application

WithinSleSnapshotWriter

##### Treatment

NoTest

```text
mapping claim; its consequences are tested by the Application invariant examples
```

##### Evidence

docs\/feature\/story\-6563\-sle\-attainment\/design\/withinSleDailyRecord\.qnt

### Refuted

None.

### Claims

#### C1

```text
A recorded day keeps its share and the SLE in force that day; no SLE change and no fill rewrites it
```

```text
history silently rewritten: the chart would show past days against an SLE they were never held to
```

WithinSleSnapshotWriter

```text
refresh events, gap asks for missing days, SLE settings saves
```

WithinSleSnapshotWriter\.RecordToday \/ FillDayIfAbsent

IWithinSleOverTimeSeriesQuery

```text
within-sle-over-time Days[*].SlePercentile, SleDays, Origin
```

```text
any other writer of the WithinSleSnapshots table (direct EF use, a future migration data step, a database restore); covered only by the single-writer convention and the module layout test, not by the C# type checker
```

#### C2

```text
A Measured share always has at least 5 closed Work Items and an SLE
```

```text
a pass or fail verdict on too little data (breaks Q5)
```

WithinSleShare

```text
closed cycle times and the owner SLE settings
```

WithinSleShare\.Of

WithinSleInfoQuery\, WithinSleOverTimeSeriesQuery

```text
Verdict field of both answers
```

```text
C# construction is centralised by a private constructor, not proved; reflection could build one; the frontend trusts Verdict and must not recount
```

### States

Admitted

```text
stored day Recorded, Measured (>= 5 closed)
```

None.

Admitted

```text
stored day Recorded or FilledIn with fewer than 5 closed (TooFew, kept for the count, no point)
```

None.

Admitted

```text
stored day FilledIn against the SLE current at fill time
```

None.

Excluded

```text
a stored row without an SLE
```

None.

Excluded

```text
WithinCount greater than ClosedCount
```

None.

Excluded

```text
two rows for one owner, horizon and day
```

None.

Excluded

```text
a past Recorded row changed after its day ended
```

None.

Unknown

```text
which SLE was set on a filled-in day before recording started (Q4 accepts today SLE)
```

Product

### Models

Quint

docs\/feature\/story\-6563\-sle\-attainment\/design\/withinSleDailyRecord\.qnt

##### Properties

pastDaysNeverChange

Invariant

```text
a day that ended with a row keeps that row
```

WithinSleSnapshotWriter\.FillDayIfAbsent

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleSnapshotWriter\.cs

laterRowsOfPastDaysAreFilledIn

Invariant

```text
a past day that ended without a row is only ever FilledIn
```

WithinSleSnapshotOrigin\.FilledIn

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WithinSleSnapshot\.cs

withinNeverExceedsClosed

Invariant

```text
within <= closed on every row
```

WithinSleShare\.Of

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/WithinSleShare\.cs

noPointBelowFive

Invariant

```text
a row draws a point only with at least 5 closed
```

WithinSleShare\.MinimumClosed

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/WithinSleShare\.cs

##### Atoms

Action

recordToday

WithinSleSnapshotWriter\.RecordToday

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleSnapshotWriter\.cs

None.

Action

fillDayIfAbsent

WithinSleSnapshotWriter\.FillDayIfAbsent

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleSnapshotWriter\.cs

None.

Action

changeSle

WorkTrackingSystemOptionsOwner\.ServiceLevelExpectationRange

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WorkTrackingSystemOptionsOwner\.cs\:41

None.

Action

advanceDay

ILighthouseClock\.Today

Lighthouse\.Backend\/Lighthouse\.Backend\/Platform\/Host

None.

State

stored

WithinSleSnapshot

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WithinSleSnapshot\.cs

None.

State

currentSle

ServiceLevelExpectation\.For

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/ServiceLevelExpectation\.cs

None.

State

closedDays

model\-only

docs\/feature\/story\-6563\-sle\-attainment\/design\/withinSleDailyRecord\.qnt

None.

Constant

MIN\_CLOSED

WithinSleShare\.MinimumClosed

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/WithinSleShare\.cs

None.

##### Assumptions

```text
one owner, one horizon, days 1..3, two SLE values or none, closed counts 0..6
```

```text
two app copies and crash windows are not modelled; covered by the unique index and the SQLite service-port test
```

##### Mutants

###### N1

```text
fill without the absence check overwrites an existing day
```

###### Kills

pastDaysNeverChange

###### N2

```text
fill marks its row Recorded
```

###### Kills

laterRowsOfPastDaysAreFilledIn

###### N3

```text
within not bounded by closed
```

###### Kills

withinNeverExceedsClosed

###### N4

```text
a point is drawn at any count
```

###### Kills

noPointBelowFive

### Closure

```text
daily record keeps each day SLE and share
```

Safety

Pass

DesignedNotBuilt

```text
Quint run per invariant and verify allInvariants NoError at 6 steps; each invariant killed by its mutant
```

##### Evidence

docs\/feature\/story\-6563\-sle\-attainment\/design\/withinSleDailyRecord\.qnt

docs\/feature\/story\-6563\-sle\-attainment\/design\/withinSleDailyRecord\.receipt\.txt

```text
Within SLE widget and chart
```

Inhabitation

Pass

DesignedNotBuilt

```text
closed cycle times + ServiceLevelExpectation.For(owner) -> WithinSleShare.Of -> WithinSleInfoQuery / WithinSleSnapshotWriter -> WithinSleSnapshot -> WithinSleOverTimeSeriesQuery; every value built from admitted input, no cycle
```

##### Evidence

None.

```text
Within SLE widget and chart
```

PublicReachability

Pass

DesignedNotBuilt

```text
withinSleInfo and within-sle-over-time on both metrics controllers -> MetricsService.getWithinSleInfo / getWithinSleOverTime -> WithinSleWidget (flow-overview) and WithinSleOverTimeWidget (predictability) through widgetNodes.tsx and categoryMetadata.ts
```

##### Evidence

None.

```text
fill-in of missing days
```

Liveness

Pass

DesignedNotBuilt

```text
not applicable: no promise of eventual fill; each pass is bounded and resumes on the next chart load, so no liveness property is claimed
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
assumption: the owner refreshes every day; negated: no refresh for a week
```

```text
recorded days
```

```text
missing days filled from history as FilledIn, open markers and the note
```

```text
interpolated or carried-forward points
```

```text
filled days use the SLE set at fill time (Q4)
```

Product

#### Y2

Stressor

```text
assumption: the SLE is stable within a day; negated: changed between two refreshes of the same day
```

```text
all past days
```

```text
today row takes the SLE of the last recording that day
```

```text
a past day re-measured against the new SLE
```

```text
none
```

FlowMetrics

#### Y3

Stressor

```text
assumption: one application copy; negated: two copies fill the same day
```

```text
one row per owner, horizon, day
```

```text
the second insert is dropped
```

```text
a duplicate point or a failed pass
```

```text
none
```

FlowMetrics

#### Y4

Stressor

```text
assumption: tracker history is stable; negated: items reopened or deleted after a day was recorded
```

```text
recorded day counts
```

```text
recorded and freshly computed values disagree
```

```text
rewriting recorded days
```

```text
accepted: recorded days show what was observed then
```

Product

#### Y5

Stressor

```text
assumption: ReconstructionMemo forgets on refresh; negated: SLE set after a day was declined while no SLE was set
```

```text
other days
```

```text
that day stays missing until the next refresh clears the memo
```

```text
a row without an SLE
```

```text
gap until next refresh
```

FlowMetrics

#### Y6

SinglePointOfFailure

```text
OverTimeHistoryFiller is the single reader of the fill queue
```

```text
recorded days and the chart answer
```

```text
a failed pass is logged and the next chart load asks again
```

```text
an exception ending the reader loop
```

```text
none
```

FlowMetrics

#### Y7

Bypass

```text
any writer of WithinSleSnapshots other than WithinSleSnapshotWriter (EF context, migrations, restore of an older database)
```

```text
the single-writer convention
```

```text
restore replaces history as a whole
```

```text
a second production writer
```

```text
not enforced by the type checker; the module layout test lists the writer and repository
```

FlowMetrics

#### Y8

Stressor

```text
assumption: the instance day is the server day; negated: reader east or west of UTC
```

```text
day keys from ILighthouseClock.Today
```

```text
frontend formats days with formatLocalDate
```

```text
reducing an instant to a day in the writer
```

```text
none
```

FlowMetrics

### Architectural tests

None.

### Contract tests

None.

## Slices

### S5

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/WithinSleShare\.cs

Create

```text
owner of within rule and minimum 5
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/ServiceLevelExpectation\.cs

Create

```text
SLE value object
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleInfoQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleInfoQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/WithinSleInfoDto\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

```text
withinSleInfo action
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

```text
withinSleInfo action
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs

Extend

```text
register IWithinSleInfoQuery
```

Lighthouse\.Frontend\/src\/models\/Metrics\/WithinSleInfo\.ts

Create

None.

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts

Extend

```text
getWithinSleInfo
```

Lighthouse\.Frontend\/src\/hooks\/metricsFetchKeys\.ts

Extend

```text
withinSleInfo key
```

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.ts

Extend

```text
withinSleInfo query and fetch state
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.tsx

Create

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/categoryMetadata\.ts

Extend

```text
placement, trend policy, fetch requirements
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.ts

Extend

```text
info text, higher is better
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Extend

```text
View Data reuses the closed-items dialog
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleInfoAtTheServicePortTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Acceptance.WithinSle.WithinSleInfoAtTheServicePortTest
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/MetricsView/WithinSleWidget.test.tsx
pnpm --dir Lighthouse.EndToEndTests exec playwright test tests/specs/metrics/WithinSle.spec.ts
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.FlowMetricsModuleLayoutArchUnitTest
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.test\.tsx

Lighthouse\.EndToEndTests\/tests\/specs\/metrics\/WithinSle\.spec\.ts

Lighthouse\.EndToEndTests\/tests\/models\/metrics\/WithinSleWidget\.ts

None.

None.

#### Public oracle

```text
Team (and a Portfolio) with SLE 70 % within 10 days and 11 Work Items closed in the selected range, 8 of them with Cycle Time <= 10 days, no Premium licence; ask IWithinSleInfoQuery for the range; in the UI open the Flow Overview and select View Data on the Within SLE widget
```

```text
Verdict Measured, ClosedCount 11, WithinCount 8, SharePercent 72.7, SlePercentile 70, SleDays 10; widget body reads 72.7 % (8 / 11), of Work Items closed within 10 days or less, Target 70 %; View Data opens the closed-items dialog with the 11 Work Items, close day, Cycle Time coloured against the SLE, longest first; shown without a licence
```

```text
a Work Item at exactly 10 days counted as late, items closed outside the range counted, the share computed in the frontend, a Premium gate, or the widget missing from a Portfolio
```

##### Promises

P1

P11

P13

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

GetCycleTimeDataForTeam

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:414

Reuse

```text
the closed population and Cycle Time the sibling widgets use, with cache and filters
```

GetCycleTimeDataForPortfolio

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPortfolioMetricsService\.cs\:61

Reuse

```text
same for Portfolios
```

calculateSLEStats

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.ts\:3

Reuse

```text
left in place for Cycle Time Percentiles only; not consumed by the new widget, whose share comes from WithinSleShare (no second owner added)
```

ServiceLevelExpectationRange

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WorkTrackingSystemOptionsOwner\.cs\:41

Reuse

```text
the SLE settings stay where they are; ServiceLevelExpectation.For reads them
```

SleRiskCalculator

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/SleRiskCalculator\.cs\:12

Reuse

```text
considered: it judges open items against the SLE days, a different question; not extended
```

WithinSleShare

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/WithinSleShare\.cs

CreateNew

```text
no backend owner of the share or the minimum of 5 exists; SleRiskCalculator answers risk of open items
```

IWithinSleInfoQuery

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleInfoQuery\.cs

CreateNew

```text
S12a shape: one read port per capability instead of growing BaseMetricsService
```

useMetricsQuery

Lighthouse\.Frontend\/src\/hooks\/useMetricsQuery\.ts\:46

Reuse

```text
keyed TanStack fetch and fetch state for the widget
```

WidgetShell

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WidgetShell\.tsx\:484

Reuse

```text
header with View Data, trend, status; loading and could-not-load looks
```

InfoWidgetComparisonDto

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/InfoWidgetDtos\.cs\:8

Reuse

```text
header trend carrier as used by every sibling
```

PortfolioMetricsController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:23

Extend

```text
existing driving adapter gains withinSleInfo
```

TeamMetricsController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:23

Extend

```text
existing driving adapter gains withinSleInfo
```

WithinSleInfoDto

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/WithinSleInfoDto\.cs

CreateNew

```text
no carrier holds verdict, counts and target; own file keeps InfoWidgetDtos.cs untouched
```

WithinSleInfoQuery

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleInfoQuery\.cs

CreateNew

```text
implements the new read port; composes the metrics services instead of extending BaseMetricsService
```

ServiceLevelExpectation

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/ServiceLevelExpectation\.cs

CreateNew

```text
no SLE value object exists; two raw ints on the owner today
```

AddFlowMetrics

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs\:9

Extend

```text
module registration point
```

metricsFetchKeys

Lighthouse\.Frontend\/src\/hooks\/metricsFetchKeys\.ts\:8

Extend

```text
one more fetch key
```

useMetricsData

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.ts\:141

Extend

```text
owner of dashboard fetches and fetch states
```

WithinSleInfo

Lighthouse\.Frontend\/src\/models\/Metrics\/WithinSleInfo\.ts

CreateNew

```text
frontend shape of the new answer
```

BaseMetricsView

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:62

Extend

```text
owns View Data dialogs; reuses the closed-items dialog
```

trendPolicies

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/categoryMetadata\.ts\:113

Extend

```text
single registry of placement, trend policy and fetch requirements
```

percentilesOverTime

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.ts\:391

Extend

```text
info text registry
```

percentilesOverTime

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx\:487

Extend

```text
widget node registry
```

WithinSleWidget

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.tsx

CreateNew

```text
no tile shows a share with count and target
```

getCycleTimePercentilesInfo

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts\:219

Extend

```text
existing metrics API client gains getWithinSleInfo beside the sibling info call
```

##### Impact

http\-endpoint

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Changed

```text
new action only; existing actions unchanged
```

http\-endpoint

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Changed

```text
new action only
```

module\-registration

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs

Changed

```text
one AddScoped line
```

frontend\-service

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts

Changed

```text
new method on IMetricsService and BaseMetricsService; every mock of IMetricsService in tests needs the method (test helpers updated by their owners)
```

widget\-registry

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/categoryMetadata\.ts

Changed

```text
categoryMetadata.test.ts requires a fetch entry and trend policy for every widget; both added
```

lighthouse\-clients

Lighthouse\-Clients

UnchangedCompatible

```text
new endpoint is additive; clients follow in a later Story (Q11)
```

value\-object

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/WithinSleShare\.cs

Changed

```text
new
```

value\-object

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/ServiceLevelExpectation\.cs

Changed

```text
new
```

read\-port

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleInfoQuery\.cs

Changed

```text
new
```

application\-service

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleInfoQuery\.cs

Changed

```text
new
```

dto

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/WithinSleInfoDto\.cs

Changed

```text
new
```

frontend\-model

Lighthouse\.Frontend\/src\/models\/Metrics\/WithinSleInfo\.ts

Changed

```text
new
```

fetch\-keys

Lighthouse\.Frontend\/src\/hooks\/metricsFetchKeys\.ts

Changed

```text
one key added; MetricsFetchKey union widens, existing switches must stay total
```

data\-hook

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.ts

Changed

```text
one query added; existing queries unchanged
```

view

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Changed

```text
View Data case for withinSle
```

info\-registry

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.ts

Changed

```text
one entry
```

widget\-registry\-nodes

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx

Changed

```text
one node
```

component

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.tsx

Changed

```text
new
```

##### Agreement

GET withinSleInfo \-\> WithinSleInfoDto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Producer

Migrated

```text
new contract, produced in this slice
```

GET withinSleInfo \-\> WithinSleInfoDto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Producer

Migrated

```text
same contract for Portfolios
```

GET withinSleInfo \-\> WithinSleInfoDto

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts

Consumer

Migrated

```text
only consumer, same commit
```

##### Not applicable

contract_tests

```text
producer and consumer ship in one deployable in the same commit; the primary oracle and the frontend test cover them; no external consumer until the Lighthouse-Clients Story
```

prefactoring

```text
Per touched file: TeamMetricsController.cs, PortfolioMetricsController.cs and FlowMetricsModule.cs each hold one type -> not-applicable; WithinSleShare.cs, ServiceLevelExpectation.cs, IWithinSleInfoQuery.cs, WithinSleInfoQuery.cs, WithinSleInfoDto.cs, WithinSleInfo.ts, WithinSleWidget.tsx are new -> not-applicable; MetricsService.ts holds IMetricsService, IFeatureSizeMetricsService, ITeamMetricsService, IProjectMetricsService and BaseMetricsService -> not-applicable in this slice: the interfaces describe the one service class family, every frontend import goes through this module, and the frontend convention keeps a service interface beside its implementation, so a split is a separate frontend improvement item, not a move this slice needs; metricsFetchKeys.ts, useMetricsData.ts, categoryMetadata.ts, widgetNodes.tsx, widgetInfoMetadata.ts, BaseMetricsView.tsx hold supporting types of one module (props and key unions) -> not-applicable, no type is touched
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T1

```text
WithinSleShare and ServiceLevelExpectation live in FlowMetrics Domain (pure), IWithinSleInfoQuery in Application contracts, WithinSleInfoQuery in Application implementations; Domain reaches no Application or Adapter type
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.FlowMetricsModuleLayoutArchUnitTest
```

###### T2

```text
no new acceptance test starts a web host
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.TestHostUseGuardTest
```

###### T3

```text
every widget has a fetch requirement entry and a trend policy
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/categoryMetadata\.test\.ts

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/MetricsView/categoryMetadata.test.ts
```

##### Contract tests

None.

### S6

#### Targets

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.ts

Extend

```text
statusAgainstTarget extracted, computeWithinSleRag added
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.tsx

Extend

```text
NoSle and TooFew bodies, status colour
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Extend

```text
RAG wiring for withinSle
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleInfoQuery\.cs

Extend

```text
previous-period comparison
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/MetricsView/WithinSleWidget.test.tsx src/pages/Common/MetricsView/ragRulesTimeAndAge.test.ts
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Acceptance.WithinSle.WithinSleInfoVerdictsAtTheServicePortTest
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleInfoVerdictsAtTheServicePortTest\.cs

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.test\.ts

None.

None.

#### Public oracle

```text
render the Within SLE widget for answers: 72.7 % vs target 70 (previous 68.6 %), 60 % vs 70, 49.9 % vs 70, NoSle, TooFew(3), and a previous period TooFew
```

```text
status Sustain green, Observe amber, Act red, Act with No SLE set, no status with 3 Work Items closed - too few to judge (needs at least 5) in a neutral colour; header trend up +4.1 pts, none when either period is not Measured; Cycle Time Percentiles status unchanged for the same data
```

```text
Observe at exactly 20.1 points below, a status or trend shown for TooFew, a green colour without the status word, or a changed Cycle Time Percentiles status
```

##### Promises

P2

P3

P4

P5

P12

#### Prefactoring

```text
L2 extract function: move the Sustain/Observe/Act threshold branch of computeCycleTimePercentilesRag into statusAgainstTarget(sharePercent, targetPercent), separate refactor commit before the behaviour; makes computeWithinSleRag reuse the one Q7 rule. Per touched file: ragRulesTimeAndAge.ts holds functions only -> this move; WithinSleWidget.tsx, BaseMetricsView.tsx, WithinSleInfoQuery.cs one component or type -> not-applicable
```

```text
computeCycleTimePercentilesRag status and tip text for every input
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.test\.ts

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/MetricsView/ragRulesTimeAndAge.test.ts
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

computeCycleTimePercentilesRag

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.ts\:75

Extend

```text
its threshold branch becomes statusAgainstTarget, the one owner of Q7, reused by computeWithinSleRag
```

BuildCycleTimePercentilesInfoDto

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:1228

Reuse

```text
pattern only: same previous-period window arithmetic; not called, the base class is not extended
```

WithinSleInfoQuery

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleInfoQuery\.cs

CreateNew

```text
created in S5, extended here with the comparison
```

BaseMetricsView

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:62

Extend

```text
RAG wiring lives here for every Flow Overview widget
```

WithinSleWidget

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.tsx

CreateNew

```text
created in S5, extended here with the verdict bodies
```

##### Impact

rag\-rule

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.ts

Changed

```text
extraction preserves computeCycleTimePercentilesRag output; new rule added
```

application\-service

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleInfoQuery\.cs

Changed

```text
Comparison filled
```

view

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Changed

```text
computeWithinSleRag wired; other RAG untouched
```

component

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleWidget\.tsx

Changed

```text
NoSle and TooFew bodies, status colour
```

##### Agreement

WithinSleInfoDto\.Comparison \(InfoWidgetComparisonDto\)

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleInfoQuery\.cs

Producer

UnchangedCompatible

```text
same DTO shape all header trends read
```

WithinSleInfoDto\.Comparison \(InfoWidgetComparisonDto\)

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Consumer

UnchangedCompatible

```text
existing previous-period trend rendering
```

##### Not applicable

contract_tests

```text
same deployable, same commit; primary oracle covers producer and consumer
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T4

```text
every widget has a trend policy; withinSle is previous-period
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/categoryMetadata\.test\.ts

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/MetricsView/categoryMetadata.test.ts
```

##### Contract tests

None.

### S7

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WithinSleSnapshot\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WithinSleSnapshotOrigin\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs

Extend

```text
DbSet and unique index
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleSnapshotRepository\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Persistence\/WithinSleSnapshotRepository\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleFamilyReader\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleFamilies\.cs

Create

```text
one list for live recording and fill, like PercentileFamilies
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleSnapshotWriter\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleSnapshotWriter\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/WithinSleRecordingHandler\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleOverTimeSeriesQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleOverTimeSeriesQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/WithinSleOverTimeDto\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/WithinSleDayDto\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

```text
within-sle-over-time action
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

```text
within-sle-over-time action
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs

Extend

```text
repository, writer, query
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
WithinSleRecordingHandler after the metrics update handler
```

Lighthouse\.Frontend\/src\/models\/Metrics\/WithinSleOverTime\.ts

Create

None.

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts

Extend

```text
getWithinSleOverTime
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useWithinSleOverTime\.ts

Create

```text
keyed TanStack query
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.tsx

Create

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/categoryMetadata\.ts

Extend

```text
predictability placement after percentilesOverTime, trend none, own fetch lifecycle
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.ts

Extend

None.

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md

Extend

```text
Flow metrics glossary rows
```

ARCHITECTURE\.md

Extend

```text
section 4 daily record
```

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Migrations\/LighthouseAppContextModelSnapshot\.cs

Extend

```text
Create-Migration.ps1 AddWithinSleSnapshots regenerates it and adds the migration file
```

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Migrations\/LighthouseAppContextModelSnapshot\.cs

Extend

```text
same for Postgres
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleDailyRecordAtTheServicePortTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Acceptance.WithinSle.WithinSleDailyRecordAtTheServicePortTest
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/MetricsView/WithinSleOverTimeWidget.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.DomainEventHandlerOrderPinTest|FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.ExpandOnlyMigrationGuardTest|FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.FlowMetricsModuleLayoutArchUnitTest
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.test\.tsx

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/WithinSleShareTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/WithinSleSnapshotWriterTest\.cs

None.

None.

#### Public oracle

```text
on a real SQLite service-port harness: Team with SLE 70/10 refreshed on day 1 (8 of 11 within, trailing 30), SLE changed to 85/14, refreshed on day 2; a day with 3 closed; then IWithinSleOverTimeSeriesQuery for horizon 30 over days 1..3; a Team with no SLE; the chart rendered for these answers with the 30/60/90 toggle
```

```text
day 1 Recorded 8/11 at 70/10 Measured 72.7; day 2 Recorded against 85/14; the 3-closed day TooFew with no point and the chart says why; day 3 absent and draws no point; no-SLE owner shows the no SLE set message instead of a line; target line steps on day 2; horizon 45 refused with 400
```

```text
day 1 re-measured against 85/14, a point for the 3-closed day, a 0 % point for an absent day, a line drawn while no SLE is set, or an old horizon answer shown after switching
```

##### Promises

P6

P7

P9

P10

P14

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

IPercentileSnapshotWriter

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPercentileSnapshotWriter\.cs\:31

Reuse

```text
shape copied (RecordToday / FillDayIfAbsent / SaveFilledDay), not extended: its rows carry percentiles, not counts and an SLE
```

PercentilesOverTimeSnapshot

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/PercentilesOverTimeSnapshot\.cs\:3

Reuse

```text
considered and not extended: adding counts, SLE and origin would widen every percentile row and its client contract
```

PercentilesOverTimeRecordingHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/PercentilesOverTimeRecordingHandler\.cs\:10

Reuse

```text
pattern for events, containment and cache invalidation; its alert family stays Percentiles, so a sibling handler is created
```

LostRaceTolerantSave

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriter\.cs\:120

Reuse

```text
drops the refused duplicate day
```

IPercentilesOverTimeSeriesQuery

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPercentilesOverTimeSeriesQuery\.cs\:12

Reuse

```text
S12a read-port shape for IWithinSleOverTimeSeriesQuery
```

seriesStatusOf

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetStatus\.ts\:99

Reuse

```text
loading and failure mapping to WidgetShell status
```

PercentilesOverTimeWidget

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/PercentilesOverTimeWidget\.tsx\:30

Reuse

```text
horizon toggle, empty-state sentence and axis conventions reused; separate widget because series and target line differ
```

WithinSleSnapshotWriter

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleSnapshotWriter\.cs

CreateNew

```text
single writer of the new table; no existing writer owns counts with an SLE
```

Flow metrics

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:17

Extend

```text
glossary rows added to the existing Flow metrics context
```

FlowMetrics

ARCHITECTURE\.md\:100

Extend

```text
section 4 names the daily record
```

PortfolioMetricsController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:23

Extend

```text
within-sle-over-time action
```

TeamMetricsController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:23

Extend

```text
within-sle-over-time action
```

ProcessBehaviorSnapshots

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs\:93

Extend

```text
DbSet and index beside the other snapshot tables
```

WithinSleDayDto

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/WithinSleDayDto\.cs

CreateNew

```text
per-day answer row
```

WithinSleOverTimeDto

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/WithinSleOverTimeDto\.cs

CreateNew

```text
series answer with current SLE
```

WithinSleSnapshot

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WithinSleSnapshot\.cs

CreateNew

```text
see ADR: counts with the SLE in force, not a percentile row
```

WithinSleSnapshotOrigin

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WithinSleSnapshotOrigin\.cs

CreateNew

```text
recorded versus filled-in has no carrier today
```

WithinSleSnapshotRepository

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Persistence\/WithinSleSnapshotRepository\.cs

CreateNew

```text
one repository per snapshot table, like PercentilesOverTimeSnapshotRepository
```

IWithinSleOverTimeSeriesQuery

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleOverTimeSeriesQuery\.cs

CreateNew

```text
S12a read port
```

IWithinSleSnapshotRepository

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleSnapshotRepository\.cs

CreateNew

```text
driven port for the table
```

IWithinSleSnapshotWriter

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleSnapshotWriter\.cs

CreateNew

```text
single-writer port
```

WithinSleRecordingHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/WithinSleRecordingHandler\.cs

CreateNew

```text
own alert family and containment
```

WithinSleFamilies

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleFamilies\.cs

CreateNew

```text
one family list for live recording and fill
```

WithinSleFamilyReader

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleFamilyReader\.cs

CreateNew

```text
reader record for the family
```

WithinSleOverTimeSeriesQuery

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleOverTimeSeriesQuery\.cs

CreateNew

```text
stored read
```

AddFlowMetrics

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs\:9

Extend

```text
registrations
```

ProcessBehaviorRecordingHandler

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1426

Extend

```text
handler registered beside the other recorders
```

LighthouseAppContextModelSnapshot

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Migrations\/LighthouseAppContextModelSnapshot\.cs\:13

Extend

```text
regenerated by Create-Migration.ps1 with the new migration
```

LighthouseAppContextModelSnapshot

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Migrations\/LighthouseAppContextModelSnapshot\.cs\:15

Extend

```text
regenerated by Create-Migration.ps1 with the new migration
```

WithinSleOverTime

Lighthouse\.Frontend\/src\/models\/Metrics\/WithinSleOverTime\.ts

CreateNew

```text
frontend shape of the series
```

trendPolicies

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/categoryMetadata\.ts\:113

Extend

```text
predictability placement
```

useWithinSleOverTime

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useWithinSleOverTime\.ts

CreateNew

```text
keyed TanStack query; usePercentilesOverTime keeps its own state cache
```

percentilesOverTime

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.ts\:391

Extend

```text
info text
```

percentilesOverTime

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx\:487

Extend

```text
node
```

WithinSleOverTimeWidget

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.tsx

CreateNew

```text
chart
```

getCycleTimePercentilesInfo

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts\:219

Extend

```text
getWithinSleOverTime
```

##### Impact

database\-schema

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs

Changed

```text
additive table and index; ExpandOnlyMigrationGuardTest
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Changed

```text
two handler registrations; forces the full backend Integration suite
```

test\-pin

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

Changed

```text
TeamDataRefreshed and PortfolioFeaturesRefreshed orders list the new handler; owned by the acceptance designer
```

database\-backup

Lighthouse\.Backend\/Restore\-DbBackup\.ps1

UnchangedCompatible

```text
restore runs migrations; an older backup gains the empty table
```

percentiles\-over\-time

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PercentileSnapshotWriter\.cs

UnchangedCompatible

```text
not touched
```

prose

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md

Changed

```text
glossary rows
```

prose

ARCHITECTURE\.md

Changed

```text
section 4
```

http\-endpoint

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Changed

```text
new action
```

http\-endpoint

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Changed

```text
new action
```

dto

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/WithinSleDayDto\.cs

Changed

```text
new
```

dto

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/WithinSleOverTimeDto\.cs

Changed

```text
new
```

entity

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WithinSleSnapshot\.cs

Changed

```text
new
```

entity

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WithinSleSnapshotOrigin\.cs

Changed

```text
new
```

repository

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Persistence\/WithinSleSnapshotRepository\.cs

Changed

```text
new
```

read\-port

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleOverTimeSeriesQuery\.cs

Changed

```text
new
```

driven\-port

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleSnapshotRepository\.cs

Changed

```text
new
```

writer\-port

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IWithinSleSnapshotWriter\.cs

Changed

```text
new
```

policy

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/Policies\/WithinSleRecordingHandler\.cs

Changed

```text
new
```

family

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleFamilies\.cs

Changed

```text
new
```

family

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleFamilyReader\.cs

Changed

```text
new
```

application\-service

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleOverTimeSeriesQuery\.cs

Changed

```text
new
```

writer

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleSnapshotWriter\.cs

Changed

```text
new
```

module\-registration

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs

Changed

```text
repository, writer, query
```

migration

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Migrations\/LighthouseAppContextModelSnapshot\.cs

Changed

```text
additive table
```

migration

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Migrations\/LighthouseAppContextModelSnapshot\.cs

Changed

```text
additive table
```

frontend\-model

Lighthouse\.Frontend\/src\/models\/Metrics\/WithinSleOverTime\.ts

Changed

```text
new
```

widget\-registry

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/categoryMetadata\.ts

Changed

```text
predictability entry, trend none, empty fetch requirement like percentilesOverTime
```

hook

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useWithinSleOverTime\.ts

Changed

```text
new
```

info\-registry

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.ts

Changed

```text
one entry
```

widget\-registry\-nodes

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx

Changed

```text
one node
```

component

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.tsx

Changed

```text
new
```

frontend\-service

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts

Changed

```text
new method; IMetricsService mocks need it
```

##### Agreement

GET within\-sle\-over\-time \-\> WithinSleOverTimeDto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Producer

Migrated

```text
new contract
```

GET within\-sle\-over\-time \-\> WithinSleOverTimeDto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Producer

Migrated

```text
new contract
```

GET within\-sle\-over\-time \-\> WithinSleOverTimeDto

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts

Consumer

Migrated

```text
only consumer, same commit
```

WithinSleSnapshots table

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/WithinSleSnapshotWriter\.cs

Producer

Migrated

```text
single writer
```

WithinSleSnapshots table

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Persistence\/WithinSleSnapshotRepository\.cs

Consumer

Migrated

```text
reads for the series query
```

##### Not applicable

contract_tests

```text
API producer and consumer in one deployable and commit; persisted rows have one writer and one reader in the same assembly, version-crossing is covered by the expand-only migration guard and the round-trip law
```

prefactoring

```text
Per touched file: TeamMetricsController.cs, PortfolioMetricsController.cs, FlowMetricsModule.cs, Program.cs, LighthouseAppContext.cs each hold one type -> not-applicable; MetricsService.ts several interfaces plus one class -> not-applicable, reason as in S5; categoryMetadata.ts, widgetNodes.tsx, widgetInfoMetadata.ts supporting types of one module, none touched -> not-applicable; every other file is new -> not-applicable; ADR-270 and ARCHITECTURE.md are prose
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T6

```text
new types sit in their FlowMetrics layer: WithinSleSnapshotWriter, WithinSleOverTimeSeriesQuery implementations; IWithinSleSnapshotWriter, IWithinSleSnapshotRepository, IWithinSleOverTimeSeriesQuery contracts; WithinSleRecordingHandler in Policies; WithinSleSnapshotRepository in Adapters/Persistence and seen by every repository rule
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.FlowMetricsModuleLayoutArchUnitTest
```

###### T7

```text
refresh handlers run in the pinned order with the metrics update first
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.DomainEventHandlerOrderPinTest
```

###### T8

```text
the migration is additive only
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ExpandOnlyMigrationGuardTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.ExpandOnlyMigrationGuardTest
```

##### Contract tests

None.

### S8

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeFillTarget\.cs

Extend

```text
WithinSleFamilies
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/RetentionEdge\.cs

Extend

```text
Guard(WithinSleFamilyReader)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFiller\.cs

Extend

```text
third family in FillOneDayAsync
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/GapAskingWithinSleOverTimeSeriesQuery\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs

Extend

```text
interface resolves to the gap-asking wrapper
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.tsx

Extend

```text
open markers and the note
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/WithinSleFillInAtTheServicePortTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Acceptance.WithinSle.WithinSleFillInAtTheServicePortTest|FullyQualifiedName~Lighthouse.Backend.Tests.Acceptance.WithinSle.TwoCopiesFillingTheSameWithinSleDayAtThePortTest
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/MetricsView/WithinSleOverTimeWidget.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.OverTimeReconstructionSeamArchUnitTest|FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.FlowMetricsModuleLayoutArchUnitTest
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.test\.tsx

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WithinSle\/TwoCopiesFillingTheSameWithinSleDayAtThePortTest\.cs

None.

None.

#### Public oracle

```text
Team with SLE 70/10, closed Work Items stored back 20 days, day 5 already Recorded at 60/8; ask IWithinSleOverTimeSeriesQuery for days 1..10 horizon 30, drain OverTimeHistoryFiller, ask again; render the answer
```

```text
first answer holds only day 5; after the drain days 1..10 inside the stored history are FilledIn against 70/10 and day 5 still Recorded at 60/8; days before the first finished Work Item absent; filled days draw open markers, recorded days filled markers, and the note says filled-in days are measured against today SLE
```

```text
day 5 rewritten, a filled day carrying Origin Recorded, a day before the history filled with 0 %, or filled and recorded markers alike
```

##### Promises

P8

P15

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

OverTimeHistoryFiller

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFiller\.cs\:20

Extend

```text
one pass fills every chart of an owner; a second filler would split budgets and the maintenance gate
```

OverTimeFillTarget

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeFillTarget\.cs\:20

Extend

```text
holds every family of an owner
```

Guard

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/RetentionEdge\.cs\:63

Extend

```text
same retention edge for the new family
```

GapAskingPercentilesOverTimeSeriesQuery

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/GapAskingPercentilesOverTimeSeriesQuery\.cs\:11

Reuse

```text
wrapper pattern; IOverTimeGapReconciler reused unchanged
```

ReconstructionMemo

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ReconstructionMemo\.cs\:21

Reuse

```text
per owner and day across families, unchanged
```

GapAskingWithinSleOverTimeSeriesQuery

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/GapAskingWithinSleOverTimeSeriesQuery\.cs

CreateNew

```text
wrapper like GapAskingPercentilesOverTimeSeriesQuery
```

AddFlowMetrics

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs\:9

Extend

```text
interface resolves to the wrapper
```

WithinSleOverTimeWidget

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.tsx

CreateNew

```text
created in S7, extended here with open markers and the note
```

##### Impact

background\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFiller\.cs

Changed

```text
third write per day; percentile and process-behaviour behaviour unchanged
```

fill\-target

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Application\/OverTimeFillTargetTest\.cs

Changed

```text
record gains a member; test constructs it
```

reconciler

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeGapReconciler\.cs

UnchangedCompatible

```text
asked with the within-SLE held days
```

query\-wrapper

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/GapAskingWithinSleOverTimeSeriesQuery\.cs

Changed

```text
new
```

fill\-target

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeFillTarget\.cs

Changed

```text
WithinSleFamilies member
```

retention

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/RetentionEdge\.cs

Changed

```text
Guard overload; existing guards unchanged
```

module\-registration

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/FlowMetricsModule\.cs

Changed

```text
registration swap
```

component

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WithinSleOverTimeWidget\.tsx

Changed

```text
markers and note
```

##### Agreement

OverTimeFillTarget \(internal record\)

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/OverTimeFillTarget\.cs

Producer

Migrated

```text
adds WithinSleFamilies
```

OverTimeFillTarget \(internal record\)

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Adapters\/Background\/OverTimeHistoryFiller\.cs

Consumer

Migrated

```text
reads it, same commit
```

##### Not applicable

contract_tests

```text
internal record and in-process ports, one deployable, same commit
```

prefactoring

```text
Per touched file: OverTimeFillTarget.cs one record, RetentionEdge.cs one class, OverTimeHistoryFiller.cs one class with private nested helpers, FlowMetricsModule.cs one class, WithinSleOverTimeWidget.tsx one component -> not-applicable; GapAskingWithinSleOverTimeSeriesQuery.cs new -> not-applicable
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T5

```text
reconstruction stays behind the filler seam; the wrapper is the only gap asker of the new series
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OverTimeReconstructionSeamArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj --filter FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.OverTimeReconstructionSeamArchUnitTest
```

##### Contract tests

None.

## ADRs

- 003 Within SLE daily record stores the SLE in force — Proposed
