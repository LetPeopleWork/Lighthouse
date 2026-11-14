# Product brief: story\-6361\-s03c\-a\-rules\-first

## Request

```text
Story #6361 S03c Package Forecasting, nwdes feature A of three (rules first). Before any Forecasting type moves into Modules/Forecasting, make the module rules shared and make every architecture rule that names Forecasting's namespaces, folders or files also see Modules/Forecasting, and add a safety net that shows forecasts and forecast answers do not change. Behaviour-free: routes, DTOs, responses and forecast results stay the same. Design: docs/feature/story-6361-s03c-package-forecasting/feature-delta.md (slices A1-A3, Ratchet design).
```

## Work

M

```text
The Forecasting move (features B and C) can start with every rule already watching the new place and a capture that proves nothing changed
```

### Rework

None.

## Promises

### P1

Outcome

```text
The module rules are one shared class that any module can use, and Flow metrics' rules still catch the same made-up offenders through it
```

### P2

Outcome

```text
Every architecture rule that names Forecasting's namespaces, folders or files also sees Modules/Forecasting: a made-up type placed there is caught by each rule
```

### P3

Outcome

```text
A pinned-seed check shows forecasts give the same percentiles, and a public command captures and compares the forecast answers on demo data before and after a move
```

## Scope

```text
Lift the Flow metrics Application rule shapes into a shared ModuleRules class; widen every Forecasting-keyed rule to see Modules/Forecasting; a pinned-seed forecast check and a forecast-answers-compare command
```

```text
Moving any Forecasting type (feature B)
```

```text
Removing the Flow metrics to Forecasting cycle and Forecasting's own module rules (feature C)
```

```text
Any behaviour, route, DTO, schema or response change
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
Flow metrics' module rules run through one shared ModuleRules class and still fail on the same made-up offenders
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.

### S2

```text
A made-up type under Modules/Forecasting is caught by every rule that names Forecasting today
```

#### Promises

P2

#### Depends on

S1

#### Slice work

None.

### S3

```text
Seeded forecasts give the same percentiles, and forecast-answers-compare reports identical answers on demo data
```

#### Promises

P3

#### Depends on

None.

#### Slice work

None.
