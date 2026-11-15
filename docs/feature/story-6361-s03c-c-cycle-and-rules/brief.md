# Product brief: story\-6361\-s03c\-c\-cycle\-and\-rules

## Request

```text
Story #6361 S03c Package Forecasting, nwdes feature C of three (cycle and rules). First, a public examine command shows forecasts being recomputed after a Team refresh and the start-up forecast seeding firing, on demo data, so the examiner can observe the moved triggers, updater and seeder working. Then break the Flow metrics to Forecasting cycle by inverting Flow metrics' three edges through ports Flow metrics owns (IThroughputFilter, IForecastDataSufficiency, IHowManyForecaster), answered by Forecasting; Forecasting keeps depending on Flow metrics. Give Forecasting its own module rules (types in their layer, Domain reaches only the kernel, outside reaches Domain, Adapters and Application only from the lists, Application outbound) with inline lists, take the Forecasting entries off Flow metrics' inbound list and empty its Forecasting outbound list, and make the shared rule wording a parameter. Move Forecasting's unit tests to Lighthouse.Backend.Tests/Modules/Forecasting with a placement test. Behaviour-free: routes, DTOs, responses, metrics answers, forecast results, handler order, DI lifetimes and the schema stay the same. Design: docs/feature/story-6361-s03c-package-forecasting/feature-delta.md (slices C1-C3, D-3, D-6, D-9, Ratchet design R-F1..R-F6, Notes from the feature B reviews).
```

## Work

M

```text
Forecasting is a module of its own with its own rules, and Flow metrics no longer depends on it, so the Flow metrics and Forecasting cycle is gone and Epic 6474 slice 07 can write into Forecasting's seam
```

### Rework

None.

## Promises

### P1

Outcome

```text
An examiner can run a public command that shows, on demo data, forecasts recomputed after a Team refresh and the start-up forecast seeding firing
```

### P2

Outcome

```text
No Flow metrics type references a Forecasting type: Flow metrics asks through three ports it owns, which Forecasting answers
```

### P3

Outcome

```text
Forecasting has its own module rules with inline lists, Flow metrics' inbound list no longer names Forecasting types, and the shared rule wording names the module it checks
```

### P4

Outcome

```text
Forecasting's unit tests live under Lighthouse.Backend.Tests/Modules/Forecasting and a placement test keeps them there
```

### P5

Outcome

```text
Metrics answers, forecasts and forecast answers are identical before and after the change, and forecasts still refresh after a Team refresh and at start-up
```

## Scope

```text
A public examine command for the forecast triggers; three Flow metrics ports and their Forecasting answers; Program.cs registrations; Forecasting module rules and the Flow metrics list edits; the shared rule wording; moving Forecasting's unit tests and a placement test
```

```text
Any behaviour, route, DTO, schema, handler-order, DI-lifetime or response change
```

```text
Entities and DTOs in Models and API/DTO, controllers, Delivery forecast publishing, LeafCapacity (Epic 6474 slice 07), an AddForecasting registration extension (S09a)
```

```text
Removing BaseMetricsService's service-locator call (S07b)
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
A public command shows forecasts recomputed after a Team refresh and the start-up seeding firing, on demo data
```

#### Promises

P1

P5

#### Depends on

None.

#### Slice work

None.

### S2

```text
Flow metrics reaches Forecasting only through its own three ports; metrics and forecast answers unchanged
```

#### Promises

P2

P5

#### Depends on

S1

#### Slice work

None.

### S3

```text
Forecasting's own module rules hold with inline lists; Flow metrics' lists no longer name Forecasting types
```

#### Promises

P3

P5

#### Depends on

S2

#### Slice work

None.

### S4

```text
Forecasting's unit tests sit under Tests/Modules/Forecasting and a placement test keeps them there
```

#### Promises

P4

P5

#### Depends on

S3

#### Slice work

None.
