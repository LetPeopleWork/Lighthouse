# Architecture brief: story\-6655\-outlier\-toggle\-and\-unpl

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Where the rule lives: the outlier rule moves to the backend, Flow metrics module, behind a replaceable definition. Domain interface ICycleTimeOutlierDefinition (Modules/FlowMetrics/Domain/CycleTimeOutlierDefinition.cs) has one method: OutlierVerdict Judge(IReadOnlyList<int> baselineCycleTimes, IReadOnlyList<CycleTimeObservation> shown). The only implementation now is UpperNaturalProcessLimitOutlierDefinition, registered once as a singleton in Program.cs next to the metrics services and injected by constructor into TeamMetricsService and PortfolioMetricsService. A later definition is supplied by registering another implementation; the route, the DTO, the services and the frontend do not change. The frontend keeps no rule: it partitions the plotted items by the outlier ids of the verdict. Confirms the DISCUSS proposals: limit = the Cycle Time PBC UNPL (average + 2.66 x average moving range of the baseline Cycle Times, configured baseline or else the selected range), compared unrounded; only above-UNPL sets an item apart; no silent 5 x median fallback.
```
Governed slices: S2, S3

#### D2

```text
Carriers and owners. Domain (Modules/FlowMetrics/Domain): UpperNaturalProcessLimit value object in XmRCalculator.cs, holding the unrounded double, built by XmRCalculator.UpperNaturalProcessLimitOf(int[] baseline) and answering IsExceededBy(int days) as days > limit; XmRCalculator.Calculate and its large-change rule use the same object, so the PBC marker and the outlier rule share one formula and one comparison. OutlierVerdict closed choice (abstract record with two cases): Limited(UpperNaturalProcessLimit Limit, IReadOnlyList<int> OutlierWorkItemIds) and NoLimits. CycleTimeObservation(int WorkItemId, int Days) record. Application (Modules/FlowMetrics/Application): GetCycleTimeOutliers(owner, startDate, endDate, int? definitionId) on ITeamMetricsService and IPortfolioMetricsService, built once in BaseMetricsService.BuildCycleTimeOutliers: it resolves the baseline with ResolveBaseline, collects baseline and shown Cycle Times with the same window, filter (Cycle Time > 0) and order as BuildCycleTimeProcessBehaviourChart (a named definition uses each item named duration, items without one are skipped), and asks the injected definition; cached through GetFromCacheIfExists with a key that includes the definition id. API: GET cycleTime/outliers?startDate&endDate&definitionId on TeamMetricsController and PortfolioMetricsController, next to cycleTime/pbc, under the same RbacGuard read requirement; DTO CycleTimeOutliersDto (API/DTO) with Status (CycleTimeOutlierStatus Ready or NoLimits, serialised as a string), UpperNaturalProcessLimit (double or null) and OutlierWorkItemIds (int array). Frontend: ICycleTimeOutliers model, MetricsService.getCycleTimeOutliers for Team and Portfolio, and a keyed TanStack query in the chart. Per-process state: none new beyond the existing metrics cache; the switch is React component state only.
```
Governed slices: S2, S3

#### D3

```text
Rejected: reuse the existing cycleTime/pbc answer in the frontend. Its UNPL is rounded to whole days while the chart classifies with the unrounded value, so a frontend comparison disagrees at the boundary (see refuted law); it has no named-definition variant; and its LargeChange marker also covers points below the lower limit. Rejected: recompute XmR in TypeScript, a second owner of the same rule. Rejected: a frontend strategy object, because the named Cycle Times and the baseline rules live in the backend. ADR-282 decisions 1 to 3 (5 x median, frontend fiftiethPercentile) are superseded for the Cycle Time scatterplot by the new ADR; its decisions 4 to 7 (filter outliers out of the series, edge markers, chip and list, statistics untouched) stay.
```
Governed slices: S2, S3

#### D4

```text
Switch: a labelled MUI Switch, copy Hide outliers, in the scatterplot title row next to the Cycle Time selector, React useState(true) inside CycleTimeScatterPlotChart, never stored, so every mount starts on. Off: no partition, every item drawn, the y-axis is the pre-outlier axis (axisTop without a limit), chip and no-limits text hidden. On: today partition and chip. Wording finalised at the DELIVER sketch review. Confirms the DISCUSS proposal.
```
Governed slices: S1

#### D5

```text
Loading and failure looks: the verdict is a keyed TanStack query owned by the chart, key [ownerKind, ownerId, startDate, endDate, definitionId or default], fetched whenever the scatterplot is shown, independent of the PBC widget. While it is pending or failed no item is set apart and no count is shown: pending reuses the chip loading wording (Outliers / Outliers, loading, disabled) and the chart reports loading up to its WidgetShell frame through the existing widget status channel, so the frame dims under its spinner; a failed query reports failure so the frame shows its could-not-load look. A late answer for an older key never lands (the key differs; no placeholder data from another key). Confirms the DISCUSS proposal.
```
Governed slices: S2, S3

#### D6

```text
No usable limits: the verdict is NoLimits when the resolved baseline is invalid (missing, or shorter than 14 days) or holds fewer than 2 positive Cycle Times (the PBC then draws UNPL = the single value or 0 and marks nothing). The chart then sets no item apart, draws every item with the pre-outlier axis, shows no count chip and shows in the chip place the text: No outliers: the {Cycle Time} process behaviour chart has no limits for this date range. The info popover says outliers need those limits. Never a fallback to 5 x median. Confirms the DISCUSS proposal.
```
Governed slices: S3

#### D7

```text
Wording (Terminology words): help sentence An outlier is a closed {Work Item} whose {Cycle Time} is above the upper natural process limit of the {Cycle Time} process behaviour chart for this date range. List title {Work Items} with a {Cycle Time} above the upper natural process limit ({N} days), where N is the unrounded limit shown with at most one decimal (30.6, 30), so the stated limit and the listed set never disagree. Confirms the DISCUSS proposal with that precision.
```
Governed slices: S2, S3

#### D8

```text
Formal verification: the claims are pure, finite, request-response functions with no concurrency, waiting or recovery (one verdict per request). Claim-to-checker: outlier membership and NoLimits laws -> executable example tables at the application-service port plus a fast-check property on the frontend partition; no Quint/TLA+/Agda model was authored or run because the root context for this feature directed no formal model checking and a short algebraic note; the formal-proof axis of these claims is INDETERMINATE (not executed), never PASS. Liveness is not applicable: no state machine, no retry and no waiting; each request is a finite computation over a bounded list.
```

#### D9

```text
Named Cycle Time definitions use the same rule (maintainer, Confirmed): baseline dates resolved as for the default; values are each item named duration over the baseline window; Cycle Times of 0 and items without a named value are skipped, as the PBC skips 0-day items. An unknown or invalid named definition yields NoLimits; the chart already resets such a selection to the default.
```
Governed slices: S2, S3

#### D10

```text
Product checklist answers: RBAC N/A, the route sits in the existing metrics controllers under their existing TeamRead/PortfolioRead guard, so whoever reads the Metrics tab reads it. Lighthouse-Clients CLI/MCP: no change, the route is additive and no existing answer changes; a client may adopt it later. Context map: Flow metrics only, no domain event; ADR-270 Flow metrics glossary entry Outlier is rewritten (closed Work Item whose Cycle Time is above the Cycle Time UNPL, owned by ICycleTimeOutlierDefinition) and Upper natural process limit (UNPL) is added, in S2. UI surface: Metrics tab of a Team or Portfolio (ADR-271), unchanged. Open item (UNPL count useful for the 20+ outlier team) stays Open, owner maintainer, at the DELIVER sketch review; it blocks no slice.
```

#### D11

```text
DEVOPS input: no platform risk (one additive read route over data already loaded, no migration, no new external call). Usage-data proposal: name-only event OutlierHandlingSwitchedOff, appended as the next integer in UsageDataEventName (28 at the time of design) and in the frontend UsageDataEventName map, emitted when the reader switches outlier handling off; it counts how often readers want every item. Emitting belongs to S1 and S1 then lists it in docs/settings/usagedata.md; DEVOPS confirms or answers N/A.
```
Governed slices: S1

### Constraints

#### K1

```text
Baseline: the configured baseline dates or, without one, the selected date range; invalid under 14 days (owned by ResolveBaseline, unchanged). At least 2 positive baseline Cycle Times give limits. No upper bound on the range or item count beyond what the PBC already accepts.
```
Governed slices: S2, S3

#### K2

```text
An outlier is strictly above the unrounded UNPL (days > limit); Cycle Times are whole days; 0-day items are never outliers.
```
Governed slices: S2, S3

#### K3

```text
No configuration of the rule and no stored switch state; outliers only on the Cycle Time scatterplot; the PBC output, its baseline and special-cause rules are unchanged (the prefactoring keeps every PBC JSON value).
```
Governed slices: S1, S2, S3

#### K4

```text
Additive only: no database migration, no change to existing routes or DTOs; Program.cs gains one registration line, which forces the full backend Integration suite at the end of S2.
```
Governed slices: S2

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

contract_tests

```text
Producer (metrics controllers) and consumer (frontend MetricsService) ship in one deployable in the same commit; Lighthouse-Clients do not consume the route. The JSON names are pinned by the S2 wire contract support.
```

### Boundaries

```text
Backend: ITeamMetricsService/IPortfolioMetricsService.GetCycleTimeOutliers (application-service port), reached over HTTP GET cycleTime/outliers. Frontend: CycleTimeScatterPlotChart props and its Hide outliers switch.
```

#### Driven ports

```text
ICycleTimeOutlierDefinition (domain strategy)
```

```text
existing work item reads and metrics cache used by BaseMetricsService
```

```text
API -> Flow metrics Application -> Flow metrics Domain; Domain depends on nothing; frontend chart -> MetricsService -> HTTP
```

#### Failures

##### B1

```text
startDate after endDate
```

Refusal

```text
existing 400 with the start-before-end message
```

##### B2

```text
unknown Team or Portfolio id
```

Refusal

```text
existing 404
```

##### B3

```text
outlier request fails or times out in the browser
```

Indeterminate

```text
WidgetShell could-not-load look; no count, no item set apart
```

### Observations

```text
Outlier work item ids of the verdict for an owner, range and definition
```

int set

```text
order of outliers; the reason for NoLimits
```

```text
Chart: drawn items, chip count or no-limits text, y-axis top, switch state
```

rendered view

```text
which backend definition produced the verdict
```

### Equality

verdict

```text
Two verdicts are equal when both are NoLimits, or both are Limited with the same id set and the same unrounded limit.
```

### Constructors

UpperNaturalProcessLimit\.Of

XmRCalculator\.UpperNaturalProcessLimitOf\(int\[\] baselineValues\) \-\> UpperNaturalProcessLimit \(length \>\= 2\)

OutlierVerdict

OutlierVerdict\.Limited\(UpperNaturalProcessLimit\, IReadOnlyList\<int\>\) \| OutlierVerdict\.NoLimits

### Laws

None.

### Refuted

```text
Comparing Cycle Times with the UNPL that cycleTime/pbc returns yields the PBC outliers
```

```text
the DTO UNPL is rounded to whole days; the classification uses the unrounded value (carrier too coarse)
```

```text
baseline giving UNPL 30.6: the PBC returns 31; a 31-day item is above 30.6 on the PBC but not above 31
```

```text
Points marked LargeChange on the PBC are the outliers
```

```text
LargeChange also marks points below the lower limit (law scope)
```

```text
a 1-day item below an LNPL of 2 is LargeChange but is not long
```

### Claims

#### C1

```text
The scatterplot outliers are exactly the PBC points above its UNPL for the same owner, range, baseline and Cycle Time definition
```

```text
two charts on one tab disagree on which Work Items are unusual
```

UpperNaturalProcessLimit

```text
baseline Cycle Times and shown Cycle Times from BaseMetricsService
```

XmRCalculator\.UpperNaturalProcessLimitOf

XmRCalculator large\-change rule and UpperNaturalProcessLimitOutlierDefinition

```text
GET cycleTime/outliers ids versus cycleTime/pbc points above the limit
```

```text
a frontend comparison of its own; a different Cycle Time computation between WorkItemDto and the service; two answers computed from data refreshed in between
```

### States

Admitted

```text
Limited verdict with zero or more outlier ids
```

None.

Admitted

```text
NoLimits verdict: invalid baseline or fewer than 2 baseline Cycle Times
```

None.

Excluded

```text
a verdict built from the rounded UNPL or from 5 x median
```

None.

Unknown

```text
whether the UNPL count is useful for the team that showed 20+ outliers
```

maintainer

### Models

None.

### Closure

```text
outlier verdict
```

Safety

Pass

DesignedNotBuilt

```text
NoLimits is the only result without limits; Limited needs an UpperNaturalProcessLimit built from >= 2 values
```

##### Evidence

None.

```text
outlier verdict
```

Inhabitation

Pass

DesignedNotBuilt

```text
two positive baseline Cycle Times build a limit and a Limited verdict
```

##### Evidence

None.

```text
outlier verdict
```

PublicReachability

Pass

DesignedNotBuilt

```text
service port -> GET cycleTime/outliers -> MetricsService.getCycleTimeOutliers -> chart chip and drawn items
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
negated: the cycle time data and the verdict come from the same data (a refresh lands between the two requests)
```

```text
every plotted item is drawn or listed
```

```text
an outlier id not among the plotted items is ignored; a new long item is drawn until the next load
```

```text
an item neither drawn nor listed; the axis clipping a drawn item
```

```text
a brief mismatch until the next load; the axis cap never goes below the tallest drawn item
```

crafter

#### Y2

Stressor

```text
negated: the selected named definition stays valid
```

```text
no stale count
```

```text
NoLimits, then the chart resets to the default
```

```text
outliers of another definition
```

```text
none
```

crafter

#### Y3

Bypass

```text
the PBC and outlier answers read separate cache entries
```

```text
same formula and comparison by construction
```

```text
cache entries expiring at different times
```

```text
a different baseline rule
```

```text
agreement depends on both being computed from the same stored items; no extra guard
```

architect

### Architectural tests

#### T1

```text
New Flow metrics types stay in their layer: the definition, verdict and limit in Domain with no Application, EF or API dependency; the service method in Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsModuleLayoutArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Lighthouse.Backend.Tests.Architecture.FlowMetrics"
```

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

Extend

```text
Hide outliers switch, local state on by default; off draws every item with the pre-outlier axis and hides the chip
```

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts

Extend

```text
switch label copy
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierSwitch\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Charts/CycleTimeScatterPlotChart.outlierSwitch.test.tsx
```

#### Supports

None.

#### Public oracle

```text
Render CycleTimeScatterPlotChart (one render) with closed Work Items of 2, 3, 3, 4 and 60 days; read the switch, the chip and the drawn items; click Hide outliers off; then on again
```

```text
At first the switch is on, the 60-day item is not drawn and the chip says 1 outlier; switched off all 5 items are drawn, the y-axis top is at least 60 and no chip is shown; switched on again the chip and the shorter axis return
```

```text
A switch that starts off or reads a stored value, an off state that still filters or keeps the chip, or an on state that does not restore the chip fails the test
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

CycleTimeScatterPlotChart

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:176

Extend

```text
owns the outlier partition, axis and chip today; the switch gates them in the same place
```

axisTop

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts\:79

Reuse

```text
with hasOutliers false it already gives the pre-outlier axis that the off state needs
```

outlierWording

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:36

Extend

```text
single owner of outlier copy
```

##### Impact

component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:176

Changed

```text
new switch; props unchanged, so widgetNodes and BaseMetricsView are untouched
```

module

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:36

Changed

```text
adds the switch label; existing wording unchanged
```

test

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierChip\.test\.tsx\:1

UnchangedCompatible

```text
the switch starts on, so existing chip tests keep their meaning
```

##### Agreement

None.

##### Not applicable

agreement

```text
no shared contract changes: the chart props, routes and DTOs stay as they are
```

prefactoring

```text
an additive switch over existing state; no structure must move first
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

###### L1

```text
With the switch off the drawn items are all items and no chip is shown; switching on restores the partition and chip; every mount starts on
```

Invariant

Application

CycleTimeScatterPlotChart

###### Treatment

Finite

###### Cases

```text
first render: on, outlier hidden, chip 1
```

```text
off: all drawn, axis fits 60, no chip
```

```text
on again: chip and shorter axis
```

```text
a two-state switch over one fixture is a closed finite domain
```

###### Evidence

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

None.

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/XmRCalculator\.cs

Extend

```text
UpperNaturalProcessLimit value object shared by the PBC and the outlier rule
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/CycleTimeOutlierDefinition\.cs

Create

```text
ICycleTimeOutlierDefinition, UpperNaturalProcessLimitOutlierDefinition, OutlierVerdict, CycleTimeObservation
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs

Extend

```text
BuildCycleTimeOutliers on the same baseline and samples as the Cycle Time PBC
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ITeamMetricsService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPortfolioMetricsService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

```text
GET cycleTime/outliers
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

```text
GET cycleTime/outliers
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/CycleTimeOutliersDto\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
register the one outlier definition
```

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts

Extend

```text
replace the median rule with partition by verdict ids; axis cap at the limit, never below the tallest drawn item
```

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts

Extend

```text
UNPL help sentence and list title
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

Extend

```text
keyed verdict query, loading and failure reporting
```

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts

Extend

```text
getCycleTimeOutliers for Team and Portfolio
```

Lighthouse\.Frontend\/src\/models\/Metrics\/CycleTimeOutliers\.ts

Create

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx

Extend

```text
pass the verdict fetcher to the chart
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Extend

```text
build the verdict fetcher for the owner and range
```

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md

Extend

```text
glossary: Outlier rewritten, Upper natural process limit added
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersFollowTheProcessBehaviourChartTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~CycleTimeOutliers"
pnpm --dir Lighthouse.Frontend exec vitest run src/utils/charts/outliers.test.ts src/components/Common/Charts/CycleTimeScatterPlotChart.outliers.test.tsx
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/CycleTimeOutliersWireContractTest\.cs

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.test\.ts

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx

#### Public oracle

```text
At the application-service port (ServicePortHarness, FakeLighthouseClock): a Team whose baseline Cycle Times give an unrounded UNPL of 30 and whose closed Work Items in the range have 12, 30, 31 and 90 days; call GetCycleTimeOutliers(team, start, end, null) and GetCycleTimeProcessBehaviourChart(team, start, end); repeat with a baseline giving 30.6 and a 31-day item; repeat with a named Cycle Time definition
```

```text
Limited with exactly the ids of the 31 and 90 day items, equal to the PBC points whose value is above its limit; at 30.6 the 31-day item is an outlier; the named definition judges by named durations the same way
```

```text
The 5 x median rule (no outliers here), a comparison with the rounded UNPL (misses 31 at 30.6), >= instead of >, or treating below-limit LargeChange points as outliers fails a row
```

##### Promises

P1

P4

#### Prefactoring

```text
Extract the UNPL into an UpperNaturalProcessLimit value object (XmRCalculator.UpperNaturalProcessLimitOf and IsExceededBy) and use it in Calculate and the large-change rule, before any outlier code; this lets the outlier definition reuse the same formula and comparison
```

```text
every XmRResult value and every special-cause classification
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/FlowMetrics\/Domain\/XmRCalculatorTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~XmRCalculatorTest"
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

XmRCalculator

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/XmRCalculator\.cs\:15

Extend

```text
already owns the UNPL formula and the above-limit comparison; the outlier rule must use the same ones
```

BuildCycleTimeProcessBehaviourChart

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:682

Extend

```text
owns baseline resolution and the Cycle Time samples (filter > 0, order); the outlier build shares them
```

ResolveBaseline

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:573

Reuse

```text
the baseline rule is unchanged
```

ComputeCycleTimeData

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:416

Reuse

```text
already yields each item with its named durations
```

ICycleTimeOutlierDefinition

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/CycleTimeOutlierDefinition\.cs\:1

CreateNew

```text
no existing type judges outliers in the backend; the replaceable definition the maintainer asked for
```

GetCycleTimeProcessBehaviourChart

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:491

Extend

```text
the outlier route sits beside it with the same guard, range check and lookup; the PBC answer is not reused (rounded limit, no named variant)
```

findOutliers

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts\:37

Replace

```text
the median rule is superseded; partition by verdict ids keeps one rule owner in the backend
```

getCycleTimePbc

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts\:150

Extend

```text
MetricsService owns metrics routes; add getCycleTimeOutliers next to it
```

ITeamMetricsService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ITeamMetricsService\.cs\:29

Extend

```text
the Team metrics application port; the outlier query sits beside GetCycleTimeProcessBehaviourChart
```

IPortfolioMetricsService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPortfolioMetricsService\.cs\:17

Extend

```text
the Portfolio twin
```

PortfolioMetricsService

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs\:61

Extend

```text
delegates to BaseMetricsService.BuildCycleTimeOutliers with Feature Cycle Times, as it does for the PBC
```

PortfolioMetricsController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:475

Extend

```text
route beside cycleTime/pbc with the same guard and checks
```

CycleTimeOutliersDto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/CycleTimeOutliersDto\.cs\:1

CreateNew

```text
ProcessBehaviourChart carries a rounded limit and every point; no DTO carries a verdict
```

Program

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1402

Extend

```text
the composition root registers the metrics services; the definition is registered there
```

outlierWording

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:36

Extend

```text
single owner of outlier copy
```

CycleTimeScatterPlotChart

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:176

Extend

```text
owns the partition and chip; it already fetches per-definition answers for the selected Cycle Time
```

ICycleTimeOutliers

Lighthouse\.Frontend\/src\/models\/Metrics\/CycleTimeOutliers\.ts\:1

CreateNew

```text
no frontend model carries the verdict
```

buildWidgetNodes

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx\:363

Extend

```text
already passes the named-percentile fetcher to the chart; the verdict fetcher goes the same way
```

onFetchNamedCycleTimePercentiles

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:224

Extend

```text
the same owner-and-range fetcher pattern builds the verdict fetcher
```

Outlier

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:505

Extend

```text
the existing glossary row is rewritten, UNPL row added
```

##### Impact

domain

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/XmRCalculator\.cs\:15

Changed

```text
limit extracted; every XmRResult value unchanged
```

application\-port

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ITeamMetricsService\.cs\:29

Changed

```text
new method; test doubles implementing the interface gain it
```

application\-port

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPortfolioMetricsService\.cs\:17

Changed

```text
new method
```

route

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:491

Changed

```text
additive route; cycleTime/pbc unchanged
```

route

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:475

Changed

```text
additive route
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1402

Changed

```text
one singleton registration; forces the full backend Integration suite
```

frontend\-rule

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts\:37

Migrated

```text
median rule and fiftiethPercentile removed; existing outliers tests rewritten by the acceptance designer
```

frontend\-service

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts\:150

Changed

```text
new call; test fakes of the metrics service gain it
```

usage\-docs

docs\/product\/architecture\/adr\-282\-outlier\-is\-a\-per\-chart\-median\-multiple\.md\:1

Migrated

```text
decisions 1 to 3 superseded by the new ADR
```

domain

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/CycleTimeOutlierDefinition\.cs\:1

Changed

```text
new file
```

application

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:682

Changed

```text
new BuildCycleTimeOutliers sharing the PBC samples; PBC output unchanged
```

application

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs\:210

Changed

```text
implements the new port method; constructor gains the definition
```

application

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs\:61

Changed

```text
implements the new port method; constructor gains the definition, so tests that new it up pass the UNPL definition
```

dto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/CycleTimeOutliersDto\.cs\:1

Changed

```text
new DTO
```

frontend\-copy

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:36

Changed

```text
help sentence and list title name the UNPL; OUTLIER_MEDIAN_MULTIPLE import removed
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:176

Changed

```text
new fetcher prop and keyed query; existing outlier tests rewritten for the new rule
```

frontend\-model

Lighthouse\.Frontend\/src\/models\/Metrics\/CycleTimeOutliers\.ts\:1

Changed

```text
new model
```

wiring

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx\:363

Changed

```text
context gains the fetcher; tests that mock the chart are unaffected
```

wiring

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:224

Changed

```text
builds the fetcher for Team and Portfolio
```

docs

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:505

Changed

```text
glossary rows
```

##### Agreement

CycleTimeOutliersDto on GET cycleTime\/outliers

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:491

Producer

Migrated

```text
new route and DTO
```

CycleTimeOutliersDto on GET cycleTime\/outliers

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs\:475

Producer

Migrated

```text
new route and DTO
```

CycleTimeOutliersDto on GET cycleTime\/outliers

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts\:150

Consumer

Migrated

```text
reads status as a string, limit as number or null, ids as numbers
```

ProcessBehaviourChart JSON on cycleTime\/pbc

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:682

Producer

UnchangedCompatible

```text
the prefactoring keeps every value
```

ProcessBehaviourChart JSON on cycleTime\/pbc

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts\:647

Consumer

UnchangedCompatible

```text
no field changes
```

##### Not applicable

None.

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L2

```text
For a valid baseline with at least 2 positive Cycle Times, the outlier ids are exactly the shown items with days strictly above the unrounded UNPL, and equal the Cycle Time PBC points above that limit; named definitions follow the same law over named durations
```

Refinement

Domain

UpperNaturalProcessLimitOutlierDefinition

###### Treatment

Finite

###### Cases

```text
UNPL 30: 12 and 30 drawn, 31 and 90 outliers
```

```text
UNPL 30.6: 31 is an outlier
```

```text
all-equal baseline (moving range 0): only items above that value
```

```text
item below the lower limit is not an outlier
```

```text
named definition with the same values gives the same ids
```

```text
the boundary rows discriminate every plausible wrong rule (median, rounded, >=, LargeChange); the backend test project has no property library and the test-writing skill puts the rule at the port
```

###### Evidence

None.

###### L3

```text
The frontend partition is total and disjoint: typical plus outliers equals the plotted items, outliers are exactly the plotted items whose id is in the verdict, sorted longest first; the axis top is at least every drawn value and at most the limit unless a drawn value exceeds it
```

Invariant

Application

outliers\.ts

###### Treatment

Broad

###### Generator

```text
lists of plotted items with unique ids and days 0..400, a verdict id set drawn from those ids plus foreign ids, a limit 0..400
```

0\.\.50 items

fast\-check default seed\, printed on failure

###### Must include

```text
empty list
```

```text
a verdict id not among the items
```

```text
a drawn item above the limit
```

None.

```text
fast-check built-in shrinking
```

```text
set union and intersection of the inputs computed in the test from the ids
```

```text
dropping an item, judging by days instead of ids, or capping the axis below a drawn item
```

###### Evidence

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

None.

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/CycleTimeOutlierDefinition\.cs

Extend

```text
NoLimits for fewer than 2 baseline Cycle Times
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs

Extend

```text
NoLimits for an invalid baseline or unknown named definition
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/CycleTimeOutliersDto\.cs

Extend

```text
NoLimits status
```

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts

Extend

```text
no-limits text
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

Extend

```text
no-limits look
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/CycleTimeOutliers\/CycleTimeOutliersWithoutProcessLimitsTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~CycleTimeOutliersWithoutProcessLimits"
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Charts/CycleTimeScatterPlotChart.outlierChip.test.tsx
```

#### Supports

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierChip\.test\.tsx

#### Public oracle

```text
At the application-service port: GetCycleTimeOutliers for a Team whose selected range is 10 days with no configured baseline and items of 5 and 90 days; again with a 30-day range holding 1 positive Cycle Time; again with a valid 14-day range holding 2
```

```text
NoLimits with no ids for the 10-day range and for 1 item, Limited for 2 items; and the rendered chart with a NoLimits verdict draws every item, shows no count chip and shows the no-limits text
```

```text
A 5 x median fallback (90 would be an outlier), treating 1 item as limits, or a count chip on NoLimits fails a row
```

##### Promises

P1

P5

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

OutlierVerdict

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/CycleTimeOutlierDefinition\.cs\:1

CreateNew

```text
created by S2 in this feature; S3 adds its NoLimits case, no earlier owner exists
```

CycleTimeOutliersDto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/CycleTimeOutliersDto\.cs\:1

CreateNew

```text
created by S2 in this feature; S3 adds the NoLimits status
```

outlierWording

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:36

Extend

```text
single owner of outlier copy
```

CycleTimeScatterPlotChart

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:176

Extend

```text
renders the no-limits look in the chip place
```

BuildCycleTimeProcessBehaviourChart

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:682

Extend

```text
S3 adds the invalid-baseline branch to the S2 outlier build, mirroring the InvalidBaselineChart branch of this method
```

outlierHeaderChip

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/outlierHeaderChip\.tsx\:1

Reuse

```text
not shown on NoLimits; the no-limits text takes its place
```

##### Impact

domain

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/CycleTimeOutlierDefinition\.cs\:1

Changed

```text
NoLimits case
```

dto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/CycleTimeOutliersDto\.cs\:1

Changed

```text
NoLimits status value, limit null
```

application

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs\:682

Changed

```text
invalid baseline or unknown named definition gives NoLimits
```

frontend\-copy

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:36

Changed

```text
no-limits text
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:176

Changed

```text
no-limits look
```

##### Agreement

CycleTimeOutliersDto status NoLimits

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs\:491

Producer

Migrated

```text
emits NoLimits with null limit and no ids
```

CycleTimeOutliersDto status NoLimits

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts\:150

Consumer

Migrated

```text
maps NoLimits to the no-limits look
```

##### Not applicable

prefactoring

```text
adds a case to the S2 choice; nothing must move first
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

###### L4

```text
The verdict is NoLimits exactly when the baseline is invalid or holds fewer than 2 positive Cycle Times; then no item is set apart
```

Invariant

Application

BaseMetricsService\.BuildCycleTimeOutliers

###### Treatment

Finite

###### Cases

```text
10-day range: NoLimits
```

```text
1 baseline item: NoLimits
```

```text
0 baseline items: NoLimits
```

```text
14 days and 2 items: Limited
```

```text
closed boundary cases of the baseline rule
```

###### Evidence

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

None.

##### Contract tests

None.

## ADRs

- 002 A Cycle Time outlier is above the Cycle Time UNPL\, judged by a replaceable backend outlier definition — Proposed
