# Acceptance brief: story\-6361\-s03c\-c\-cycle\-and\-rules

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user running the public command on demo data sees that forecasts are recomputed after a Team refresh, and that the start-up forecast seeding fires.
```

#### E2

##### Promises

P2

```text
Flow metrics code depends on no Forecasting type; every question it puts to Forecasting goes through exactly three Flow-metrics-owned ports that Forecasting answers.
```

#### E3

##### Promises

P3

```text
Forecasting has its own module rules with inline lists, Flow metrics' inbound rule list names no Forecasting type, and the shared rule's wording names the module it checks.
```

#### E4

##### Promises

P4

```text
Forecasting's unit tests sit under Lighthouse.Backend.Tests/Modules/Forecasting, and a placement test fails if they are moved elsewhere.
```

#### E5

##### Promises

P5

```text
For the same inputs, metrics answers, forecasts and forecast answers are identical before and after the change.
```

#### E6

##### Promises

P5

P1

```text
After the change, forecasts still refresh after a Team refresh and at start-up.
```

## Charter

None.

## Examination

None.
