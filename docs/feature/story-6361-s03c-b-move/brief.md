# Product brief: story\-6361\-s03c\-b\-move

## Request

```text
Story #6361 S03c Package Forecasting, nwdes feature B of three (the move). Move the 31 Forecasting files out of Services into Modules/Forecasting: the simulation, draw streams, run plan and the two forecast policies into Domain; the forecast, reality-check and filter-rule services, their ports, ForecastLane and IForecastUpdater into Application; the forecast updater and the start-up seeder into Adapters/Background; the four forecast triggers into Application/Policies. Pure moves in their own commits, namespace edits apart. The Flow metrics rule lists name the moved types in their new place. Behaviour-free: routes, DTOs, responses, forecast results, handler order and the schema stay the same. Design: docs/feature/story-6361-s03c-package-forecasting/feature-delta.md (slices B1-B3, Component decomposition, D-4, D-8).
```

## Work

M

```text
Forecasting lives in its own module folder, so feature C can give it its own rules and break the cycle with Flow metrics
```

### Rework

None.

## Promises

### P1

Outcome

```text
The simulation, draw streams, run plan and the two forecast policies live in Modules/Forecasting/Domain, which reaches only the shared kernel
```

### P2

Outcome

```text
The forecast, reality-check and filter-rule services and their ports live in Modules/Forecasting/Application, and the Flow metrics rule lists name them in their new place
```

### P3

Outcome

```text
The forecast updater and the start-up seeder live in Modules/Forecasting/Adapters/Background and the four forecast triggers in Application/Policies, with domain-event handler order unchanged
```

### P4

Outcome

```text
Forecasts and forecast answers are identical before and after the move
```

## Scope

```text
Pure move and namespace-edit commits of the 31 Forecasting files; Program.cs using lines; Flow metrics list strings; Stryker config path
```

```text
Entities and DTOs in Models and API/DTO, controllers, Delivery forecast publishing, WhatTheForecastWaitsFor (later Stories)
```

```text
Removing the Flow metrics to Forecasting cycle and Forecasting's own module rules (feature C)
```

```text
Any behaviour, route, DTO, schema, handler-order or response change
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
No Domain-layer Forecasting type remains in Services; Modules/Forecasting/Domain reaches only the kernel and the BCL
```

#### Promises

P1

P4

#### Depends on

None.

#### Slice work

None.

### S2

```text
The forecast services and their ports live in Modules/Forecasting/Application and the Flow metrics lists name them there
```

#### Promises

P2

P4

#### Depends on

S1

#### Slice work

None.

### S3

```text
The forecast updater and seeder live in Adapters/Background and the four triggers in Application/Policies; handler order unchanged
```

#### Promises

P3

P4

#### Depends on

S2

#### Slice work

None.
