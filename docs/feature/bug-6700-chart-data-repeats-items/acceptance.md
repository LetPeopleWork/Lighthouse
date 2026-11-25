# Acceptance brief: bug\-6700\-chart\-data\-repeats\-items

## Slices

### S1

##### Covers

P1

```text
A Team or Portfolio whose run-chart data has one Work Item in progress on every day of the range and two other Work Items in progress on one day each; the user opens the data list of each of the four over-time charts
```

```text
Each data list holds the repeated Work Item once and also holds both single-day Work Items
```

```text
The user sees every Work Item that was in progress during the range exactly once, not once per day
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetViewData\.repeatedItems\.test\.ts

```text
A list that keeps only the first day's items would lose the single-day Work Items of later days; the test requires all three items, so it fails
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
On each of the four over-time charts (Work Items In Progress Over Time, Total Work Item Age Over Time, Work In Progress PBC, Total Work Item Age PBC), the data list contains each Work Item that was in progress during the selected range exactly once; no Work Item name or id appears twice in the list.
```

#### E2

##### Promises

P1

```text
Every Work Item that was in progress at some point during the selected range appears in each of the four over-time charts' data list; none in progress during the range is missing from any of them.
```

#### E3

##### Promises

P1

```text
A Work Item that stays in progress across many days of the range still appears only once in each chart's data list, not once per day or per snapshot.
```

#### E4

##### Promises

P1

```text
Work Items that were never in progress during the selected range do not appear in any of the four charts' data lists.
```

#### E5

##### Promises

P1

```text
Changing the selected range and reloading a chart yields a data list in which each in-progress Work Item still appears exactly once.
```

## Charter

None.

## Examination

None.
