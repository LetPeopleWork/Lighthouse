# Product brief: story\-6358\-s02b\-flow\-metrics\-move

## Request

```text
Story 6358 S02 Flow metrics pilot, part B of 3 (DESIGN slices 2, 3a, 3b, 4, 5): move the 42 Flow metrics files out of Lighthouse.Backend.Services into Lighthouse.Backend.Modules.FlowMetrics.{Domain, Application, Application.Policies, Adapters.Background, Adapters.Persistence}, namespace and folder only, with no behaviour change. Design: docs/feature/story-6358-s02-metrics-pilot/feature-delta.md (Component decomposition, D-4 to D-9, R2, DEVOPS).
```

## Work

L

```text
The Flow metrics context lives in one module folder with its layers visible, and nothing a user sees changes
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every Flow metrics type the design names lives in its declared layer namespace under Lighthouse.Backend.Modules.FlowMetrics (Domain, Application, Application.Policies, Adapters.Background, Adapters.Persistence), in the folder that matches the namespace
```

### P2

Outcome

```text
The module's Domain depends only on the shared kernel (Lighthouse.Backend.Models) and the .NET base library: no Application, Adapters, Services, Data, EF Core or Microsoft.Extensions type
```

### P3

Outcome

```text
Nothing a user sees changes: every metrics API answer is identical before and after on the same day, the event-handler order pin stays green, the EF model is unchanged, and every existing test stays green with only using, namespace and path-string edits
```

## Scope

None.

```text
The module dependency rules R3 to R5, deleting the old Metrics type-set rule, and relocating the unit tests (feature C); extracting a registration extension (later Story); renaming *Service roles
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The five pure Flow metrics calculators (PercentileCalculator, XmRCalculator with XmRResult, SleRiskCalculator, BaselineValidationService with BaselineValidationResult) live in Lighthouse.Backend.Modules.FlowMetrics.Domain, and that Domain reaches only the shared kernel and the base library
```

#### Promises

P2

P1

P3

#### Depends on

None.

#### Slice work

None.

### S2

```text
The Flow metrics contract (the two metrics-service interfaces with their records, the snapshot writer, series query, gap reconciler, history-filler and snapshot-repository interfaces) lives in Lighthouse.Backend.Modules.FlowMetrics.Application
```

#### Promises

P1

P3

#### Depends on

None.

#### Slice work

None.

### S3

```text
The Flow metrics implementations (the three metrics services, cache key, percentile families, blocked-count series builder, snapshot writers, series queries, gap reconciler, reconstruction memo, retention edge, fill target) live in Lighthouse.Backend.Modules.FlowMetrics.Application
```

#### Promises

P1

P3

#### Depends on

None.

#### Slice work

None.

### S4

```text
The seven Flow metrics event handlers (three recorders, two cache invalidations, two demo backfills) live in Lighthouse.Backend.Modules.FlowMetrics.Application.Policies and still run in the pinned order
```

#### Promises

P1

P3

#### Depends on

None.

#### Slice work

None.

### S5

```text
The background history filler lives in Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background and the three snapshot repositories in Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence, still recognised as repositories by every repository rule, with the EF model unchanged
```

#### Promises

P1

P3

#### Depends on

None.

#### Slice work

None.
