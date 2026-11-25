# Architecture brief: bug\-6700\-chart\-data\-repeats\-items

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetViewData\.ts

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/workItemLookup\.ts

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetViewData\.repeatedItems\.test\.ts

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/MetricsView/widgetViewData.repeatedItems.test.ts
```

#### Supports

None.

#### Public oracle

```text
A Team or Portfolio whose run-chart data has one Work Item in progress on many days of the range (it appears under several days), and the user opens the data list of each of the four over-time charts
```

```text
Each data list holds that Work Item exactly once, and still holds every other Work Item that was in progress on any day of the range
```

```text
The Work Item appears once per day it was in progress (e.g. 20 rows for 20 days), or a Work Item in progress on only one day is missing from the list
```

##### Promises

P1

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
