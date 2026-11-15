# Product brief: story\-6360\-s03b\-b\-move

## Request

```text
Story #6360 S03b Package Work Items, nwdes feature B of three (the move). Move the Work Items files out of Services into Modules/WorkItems as the membership test of feature A lists them (Moves: 43 types incl. RefreshLogService and IRefreshLogService and the six helper types; Stays: the three tracker selectors and the orphan clean-up pair): sync mode, fetch fingerprint, the rule evaluator and the dependency policies into Domain (types the Domain rule rejects go to Application, recorded); WorkItemService (whole), TeamDataService, BlockedItemService, the rule ports and field providers, the dependency services and ports, the parent-placement report and the refresh log into Application; the six Work Items event handlers into Application/Policies. Dependency types go into the Dependencies sub-folders the rules name. Pure moves in their own commits, namespace edits apart. Rule lists that name moved types by full name name them in their new place; the CI test-selection classifier follows FetchFingerprint. Behaviour-free: routes, DTOs, responses, refresh results, metrics answers, forecasts, handler order and the schema stay the same. Design: docs/feature/story-6360-s03b-package-work-items/feature-delta.md (slices B1-B3, D-1, D-8, Component decomposition, Ratchet design, DEVOPS).
```

## Work

M

```text
Work Items lives in its own module folder, so feature C can give it its own rules and turn the metrics update into a reaction
```

### Rework

None.

## Promises

### P1

Outcome

```text
Sync mode, the fetch fingerprint, the rule evaluator and the dependency policies live in Modules/WorkItems/Domain, which reaches only the shared kernel and the BCL; a type the rule rejects lives in Application, recorded
```

### P2

Outcome

```text
The Work Item, Team data, blocked, rule, dependency, parent-placement and refresh-log services and their ports live in Modules/WorkItems/Application, and every rule list that names them by full name names them in their new place
```

### P3

Outcome

```text
The six Work Items event handlers live in Modules/WorkItems/Application/Policies with domain-event handler order unchanged
```

### P4

Outcome

```text
Refresh answers, metrics answers and forecasts are identical before and after the move
```

## Scope

```text
Pure move and namespace-edit commits of the Work Items files feature A's membership test lists; Program.cs using lines; full-name rule list strings; the CI test-selection classifier path of FetchFingerprint; Stryker config paths if any
```

```text
Entities, events and DTOs in Models and API/DTO; controllers; the tracker selectors; the orphan clean-up pair; TeamUpdater and PortfolioUpdater (later Stories and Epic 6474)
```

```text
Flow metrics reacting to TeamDataRefreshed, Work Items' own module rules and moving the unit tests (feature C)
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
No Domain-layer Work Items type remains in Services; Modules/WorkItems/Domain reaches only the kernel and the BCL
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
The Work Items services and their ports live in Modules/WorkItems/Application and every full-name rule list names them there
```

#### Promises

P2

P4

#### Depends on

None.

#### Slice work

None.

### S3

```text
The six Work Items reactions live in Modules/WorkItems/Application/Policies and handler order is unchanged
```

#### Promises

P3

P4

#### Depends on

None.

#### Slice work

None.
