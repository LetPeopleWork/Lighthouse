# Product brief: story\-6365\-s03g\-a\-rules\-first

## Request

```text
ADO Story #6365 S03g Package Refinement, feature A (rules first): the Refinement membership is measured from the IL and fixed in a namespace-agnostic test, with each type's layer (Domain or Application), its old and its new namespace, and every Refinement file that stays with its owner and reason (A1); every rule pattern that sees a moving Refinement type today gives the same answer for it under Modules.Refinement, and no rule string or literal names Refinement by its old Services path except a fixed rename list that only shrinks (A2). Rules and tests only: production behaviour unchanged. DESIGN and DEVOPS: docs/feature/story-6365-s03g-package-refinement/feature-delta.md.
```

## Work

S

```text
The Refinement types can move to Modules/Refinement without any rule silently changing its answer
```

### Rework

None.

## Promises

### P1

Outcome

```text
The membership of the Refinement module is measured from the compiled code and fixed in a test: each moving Refinement type with its layer (Domain or Application), its old and its new namespace, and each Refinement file that stays (the sizing-log repository pair, the Models/Refinement types, the API controllers, validator and DTOs, WeeklyRecurrence) with its owner and reason. The test finds each listed type in its old or its new namespace, fails when a listed type is missing or a listed name does not exist, fails when a type listed as Domain reaches anything beyond the kernel, the BCL and other Refinement Domain types, and records every suppression, analyser, CI-selection, test-budget, host-user and Stryker target that names a moving type.
```

### P2

Outcome

```text
Every rule pattern in the test assembly gives the same answer for each moving Refinement type under its new Modules.Refinement namespace as under its old one, and no added alternative matches a production type outside Modules.Refinement. The rule uses of the exact old namespace (equality, DoNotResideInNamespace, the exact-namespace assertion) are rewritten over the membership map, so they keep their verdict through the move.
```

### P3

Outcome

```text
No rule constant or string literal in the test project names Refinement by its old Services.Implementation.Refinement, Services.Interfaces.Refinement or Services/Implementation/Refinement path, except the entries of a fixed rename list taken from the pre-move search; the check fails when a listed literal no longer exists or an unlisted one appears, and the list only shrinks.
```

### P4

Outcome

```text
Nothing a user, a tracker or the database sees changes: no production file moves or changes behaviour in feature A, and every existing test stays green.
```

## Scope

```text
The IL-measured Refinement membership map and its namespace-agnostic test; the namespace-move preservation law over every const regex in the test assembly, extended to a Refinement map (lifted into one shared law with the platform map if the S03f law is platform-specific), with the widening of the Refinement rule constants to old and new namespaces it requires; the rewrite of the exact-namespace uses over the map; the old-path literal sweep with its fixed rename list; the recorded re-search of suppression, analyser, CI, budget, host-user and Stryker targets.
```

```text
Moving any Refinement file or test and renaming the literals on the rename list (feature B); narrowing the rules to Modules.Refinement only, the new Refinement boundary rules R-R1 to R-R6, the unit-test placement test and the architecture documents (feature C); the sizing-log repository pair, which stays in Services/*/Repositories (D-4); Models/Refinement, which stays in the kernel (D-3); an AddRefinement extension (S09a); a refinement-answers-compare command (not created, D-10).
```

## Decisions

```text
Rigor: feature A (design slice S1) thorough, so architect, acceptance designer and crafter on opus and reviewers and EXAMINE on sonnet; features B and C standard, so sonnet for producers and haiku for reviewers. DESIGN open question 1, default taken in AFK on 2026-11-24 after a 15-minute window with no reply.
```

#### Status

Confirmed


```text
DESIGN's one nwdes feature with three slices is delivered as three nwdes features (A rules first, B move, C edges and rules), as S03d, S03e and S03f were. Approval covers an oracle's bytes, and the move and the narrowing both edit what the rules-first slice fixed (its rename list, its old-or-new membership lookup), so in one feature they would make the earlier approvals stale. The Story stays one ADO Story; the size stays S, about 2.5 days.
```

#### Status

Confirmed


```text
Start condition met: S03e (Story 6363) and S03f (Story 6364) are Closed in ADO and on main, and their EXAMINE ran accepted on 2026-11-24. Every count in the DESIGN record is re-measured in A1 on main at 8e0b2fe4e or later; a difference is recorded, not argued with.
```

#### Status

Confirmed


```text
Behaviour-free: no review window after a slice; UI sketch, loading and failure looks N/A because nothing a user sees changes; usage-data event N/A because no new outcome exists to count (DEVOPS).
```

#### Status

Confirmed


```text
EXAMINE observes through admitted probes only: architecture-rules (Refinement and all), ef-model-unchanged, backend-tests with a Refinement fragment, test-selection-show (D-10). Long probes run as background jobs.
```

#### Status

Confirmed


```text
Quint scenario exploration and formal models are not relevant: no state, ordering, concurrency or liveness claim changes; the preservation law and the membership are exhaustive checks over a finite, closed set of compiled types (D-12).
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
A1: the Refinement membership is measured from the IL and fixed in a namespace-agnostic test that finds each listed type in its old or new namespace, fails when a listed type is missing or a listed name does not exist or a Domain type reaches beyond the kernel, and records the targets that name moving types.
```

#### Promises

P1

P4

#### Depends on

None.

#### Slice work

S

```text
The move in feature B has a fixed, measured list of what moves, to which layer, and what stays with which owner
```

##### Rework

None.

### S2

```text
A2: every rule pattern gives the same answer for each moving Refinement type under its old and its new namespace, no added alternative matches a production type outside Modules.Refinement, and no rule string names Refinement by its old path outside the fixed rename list.
```

#### Promises

P2

P3

P4

#### Depends on

S1

#### Slice work

M

```text
No rule silently stops seeing a Refinement type when it moves
```

##### Rework

None.
