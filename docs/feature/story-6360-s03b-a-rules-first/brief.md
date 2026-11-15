# Product brief: story\-6360\-s03b\-a\-rules\-first

## Request

```text
Story #6360 S03b Package Work Items, nwdes feature A of three (rules first). Before any Work Items type moves into Modules/WorkItems, make every architecture rule that names Work Items' namespaces, folders, paths or types also see Modules/WorkItems, fix the membership list from the compiled code, and add a safety net that shows a refresh gives the same items, transitions, blocks and events. Behaviour-free: routes, DTOs, responses, refresh results and metrics stay the same. Design: docs/feature/story-6360-s03b-package-work-items/feature-delta.md (slices A1-A3, Ratchet design, D-1, D-10).
```

## Work

M

```text
The Work Items move (features B and C) can start with every rule already watching the new place, a fixed list of what moves, and a capture that proves a refresh did not change
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every architecture rule that names Work Items' namespaces, folders, paths or types also sees Modules/WorkItems: a made-up type placed there is caught by each rule
```

### P2

Outcome

```text
The list of Work Items types that move is measured from the compiled code and fixed in a test that fails when a listed type is missing or an unlisted Work Items type appears
```

### P3

Outcome

```text
A refresh at the service port gives the same items, transitions, blocked periods and events before and after a move, and public commands capture the refresh answers and show the Work Items reactions firing on demo data
```

## Scope

```text
Widen every Work Items-keyed architecture rule to see Modules/WorkItems; a membership test from the IL; a refresh oracle at the service port; refresh-answers-compare and refresh-reactions-show commands
```

```text
Moving any Work Items type (feature B)
```

```text
Flow metrics reacting to TeamDataRefreshed and Work Items' own module rules (feature C)
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
A made-up type under Modules/WorkItems is caught by every rule that names Work Items today
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.

### S2

```text
The membership test lists the Work Items types that move, measured from the IL, and fails on a missing or an unlisted type
```

#### Promises

P2

#### Depends on

None.

#### Slice work

None.

### S3

```text
A refresh at the service port gives the same items, transitions, blocks and events, and refresh-answers-compare reports identical answers on demo data
```

#### Promises

P3

#### Depends on

None.

#### Slice work

None.
