# Architecture brief: bug\-6656\-named\-cycle\-time\-own\-population

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/BaseMetricsService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/TeamMetricsService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/PortfolioMetricsService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/ITeamMetricsService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Application\/IPortfolioMetricsService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Metrics\/NamedCycleTimeValue\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamMetricsController\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioMetricsController\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/NamedCycleTime\/NamedCycleTimePopulationTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~NamedCycleTimePopulationTest|FullyQualifiedName~TeamMetricsControllerTest|FullyQualifiedName~PortfolioMetricsControllerTests
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamMetricsControllerTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/PortfolioMetricsControllerTests\.cs

#### Public oracle

```text
Team (and portfolio) with workflow Analysis, In Progress, Ready for Release, In Production, Done and a named definition Analysis -> Ready for Release; range 1-30 Nov. Item A reached Ready for Release 5 Nov and Done 20 Nov; item B reached Ready for Release 12 Nov and is still there; item C reached Ready for Release 28 Oct and Done 3 Nov; item D is In Progress. Ask the service for the named cycle time data, percentiles and outliers for that definition and range.
```

```text
A and B are returned with named days measured Analysis->Ready for Release and end dates 5 Nov and 12 Nov; C and D are not returned; percentiles are computed over A and B only. Without a definition, cycleTimeData still returns the items closed in range (A and C) unchanged.
```

```text
C (closed in range but crossed the end before it) is returned, B (not closed) is missing, a value has no end date, the percentiles include C, or the default cycleTimeData changes
```

##### Promises

P1

#### Prefactoring

None.

#### Records

None.

### S2

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/services\/Api\/MetricsService\.ts

Extend

None.

Lighthouse\.Frontend\/src\/models\/Metrics\/NamedCycleTime\.ts

Extend

None.

Lighthouse\.Frontend\/src\/models\/WorkItem\.ts

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useNamedPercentilesScope\.ts

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetViewData\.ts

Extend

None.

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.namedMeasure\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Charts/CycleTimeScatterPlotChart.namedMeasure.test.tsx src/pages/Common/MetricsView/namedPercentilesViewData.test.tsx
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/namedPercentilesViewData\.test\.tsx

#### Public oracle

```text
Render the Cycle Time scatterplot with default data (items A closed 20 Nov, C closed 3 Nov) and select the named measure Analysis -> Ready for Release whose fetch returns A (end 5 Nov) and B (not closed, end 12 Nov); also a fetch still pending, and a fetch that fails; click a dot.
```

```text
Named: dots for A at 5 Nov and B at 12 Nov, none for C; x-axis label 'Date reached Ready for Release'; pending fetch shows the dimmed-with-spinner look, failed fetch shows the could-not-load message and no stale dots as current; dialog title names the end state and the end date. Default measure unchanged.
```

```text
Named view plots dots on the closed date, still shows C, omits B, shows the previous answer as current while loading or after a failure, or changes the default view
```

##### Promises

P2

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
