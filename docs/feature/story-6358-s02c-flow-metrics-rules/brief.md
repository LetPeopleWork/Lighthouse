# Product brief: story\-6358\-s02c\-flow\-metrics\-rules

## Request

```text
Story 6358 S02 Flow metrics pilot, part C of 3 (DESIGN slices 6, 7 and 9; slice 8 mutation is a host task on frozen code): replace the Metrics type-set rule and the hand-written opcode scanner with namespace rules R1, R3, R4 and R5 read from the compiled IL, each with an inline two-way baseline and a made-up-types fixture that fails on a newcomer and on a stale entry; update the path-string tests; relocate about 35 Flow metrics unit-test files under Lighthouse.Backend.Tests/Modules/FlowMetrics as a pure move; then record the pilot in ADR-270, ARCHITECTURE.md section 4, brief.md and the Epic catalogue. No behaviour change. Design: docs/feature/story-6358-s02-metrics-pilot/feature-delta.md (Ratchet design R1 to R7, Slices 6 to 9, SSOT edits pending, DEVOPS).
```

## Work

L

```text
The Flow metrics module's dependencies are ratcheted by namespace rules, its tests sit next to its name, and the architecture records say what the pilot taught
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every edge into and out of the Flow metrics module is named: outside types reach its Domain only from the listed types and its Adapters only from the composition root (R3), reach its Application only from API, the composition root, Reports, Refinement and the listed undeclared edges (R4), and its Application depends on no EF Core, Data, Adapters or API type and reaches Forecasting only through the listed types (R5); each rule fails on a new offender and on a stale baseline entry, also for an edge written only inside an async method
```

### P2

Outcome

```text
The old Metrics type-set rule and the hand-written opcode scanner are gone and nothing they guarded is lost: the module is found by its namespaces, and the stricter namespace rules cover what the scanner checked
```

### P3

Outcome

```text
Nothing a user sees changes: every existing test stays green, the Flow metrics unit tests sit under Lighthouse.Backend.Tests/Modules/FlowMetrics with only folder, namespace and using edits, and the EF model is unchanged
```

## Scope

None.

```text
A registration extension method for the module (later Story); read ports for the GetAllByPredicate calls (S05f); moving entities or DTOs out of Models; renaming *Service roles; moving the Metrics controllers out of API
```

```text
The ADR-270, ARCHITECTURE.md section 4, brief.md and Epic catalogue edits: nwdes accepts a prose-only slice only under its own doc folders, so these are written in Lighthouse's feature finalize checklist (docs prose and context map), not as a slice
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
Outside types reach the module's Domain only from the six listed types and nothing outside reaches its Adapters except the composition root; a made-up newcomer and a stale baseline entry each fail the rule
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.

### S2

```text
Outside types reach the module's Application only from API, the composition root, Reports, Refinement and the eight listed undeclared edges; a made-up newcomer, an async-only newcomer and a stale baseline entry each fail the rule
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.

### S3

```text
The module's Application depends on no EF Core, Data, Adapters or API type and reaches Forecasting only through the listed type; the opcode scanner and the Metrics type-set rule are deleted and the module is found by its namespaces
```

#### Promises

P1

P2

#### Depends on

None.

#### Slice work

None.

### S4

```text
The Flow metrics unit tests sit under Lighthouse.Backend.Tests/Modules/FlowMetrics, the folder matches each test's namespace, and the whole suite stays green
```

#### Promises

P3

#### Depends on

None.

#### Slice work

None.
