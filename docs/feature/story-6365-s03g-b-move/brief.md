# Product brief: story\-6365\-s03g\-b\-move

## Request

```text
ADO Story #6365 S03g Package Refinement, feature B (move): the 16 Refinement files that feature A's membership map lists move into Modules/Refinement/Domain (NeedBand.cs) and Modules/Refinement/Application (the other 15, the three port files with all their records), namespace equal to folder, with their using lines, the Program.cs registrations' full names and every literal on feature A's rename list renamed (B1); the 11 Refinement unit tests move to Tests/Modules/Refinement/Domain and /Application, namespace equal to folder, while the acceptance, wire-contract, model and usage-data tests stay (B2). Behaviour-free. DESIGN and DEVOPS: docs/feature/story-6365-s03g-package-refinement/feature-delta.md; feature A: docs/feature/story-6365-s03g-a-rules-first/.
```

## Work

S

```text
Refinement is packaged in Modules/Refinement and nothing a user, a tracker or the database sees changes
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every type that feature A's membership map (RefinementMembership) lists as moving lives in its new namespace Lighthouse.Backend.Modules.Refinement.Domain or .Application, in the folder of the same name, and in no other namespace; no file of the moving set is left in Services/Implementation/Refinement or Services/Interfaces/Refinement; the staying Refinement files (sizing-log repository pair, Models/Refinement, the API controllers, validator and DTOs, WeeklyRecurrence) are where they were.
```

### P2

Outcome

```text
Every string feature A's rename list (RefinementStringsTheMoveRenames) names is renamed to the new place or, for the rows that only existed for the old folder, removed, and the list is left with no entry for a moved type; the Program.cs Refinement registrations keep their order, lifetimes and service and implementation pairs and change only their full names; every architecture rule and feature A's preservation law stay green after the move.
```

### P3

Outcome

```text
The 11 Refinement unit tests live under Lighthouse.Backend.Tests/Modules/Refinement/Domain (the NeedBand test) and /Application (the other 10), namespace equal to folder; the Refinement acceptance tests at the service port, the wire-contract tests, the model tests, the usage-data tests and SizingLogRepositoryTest stay where they are, with only their using lines changed.
```

### P4

Outcome

```text
Nothing a user, an API client, a tracker or the database sees changes: the Refinement acceptance and wire-contract tests pass with unchanged assertions (tab, need, votes, comments, take-back, settings), the EF model is unchanged with no migration, routes, DTOs and headers are unchanged, CI selects the backend suite for a change under Modules/Refinement, and the whole filtered backend suite stays green.
```

## Scope

```text
Moving the 16 Refinement production files and the 11 Refinement unit tests, namespace equal to folder; using lines across production and tests; the Program.cs Refinement registrations' full names; renaming or removing every literal on feature A's rename list (the Access VoterIdentityResolver entry, the RefinementList.cs paths in two guard tests, the Tracker integration IRefinementViewQuery entry, the 16 PlatformLaneUnitTestPlacementTest rows) and emptying the list of moved entries.
```

```text
Narrowing the Refinement rule patterns to Modules.Refinement only, the new Refinement boundary rules R-R1 to R-R6, the Refinement unit-test placement test and the architecture documents (feature C); the sizing-log repository pair (stays, D-4); Models/Refinement (stays, D-3); an AddRefinement extension (S09a); any change of order, lifetime or pair in Program.cs; any behaviour, route, DTO, migration or frontend change.
```

## Decisions

```text
Rigor standard for feature B: architect, acceptance designer and crafter on sonnet; reviewers and EXAMINE on haiku (DESIGN open question 1, AFK default taken on 2026-11-24).
```

#### Status

Confirmed


```text
Feature B starts after feature A is accepted (whole-candidate review and EXAMINE, 2026-11-24). The move goes in refactor(refinement): move commits; the narrowing and the new rules wait for feature C. Behaviour-free: no review window, UI sketch and loading or failure looks N/A, usage-data event N/A.
```

#### Status

Confirmed


```text
EXAMINE observes through admitted probes only: architecture-rules (Refinement and all), backend-tests with Refinement fragments for the unchanged acceptance and wire-contract classes, ef-model-unchanged, test-selection-show for a path under Modules/Refinement, and the whole filtered backend-tests run as a background job (D-10). No refinement-answers-compare command (D-10, open question 3).
```

#### Status

Confirmed


```text
Quint and formal models not relevant: a namespace move of a finite, closed set of types with no state, ordering or concurrency claim (D-12).
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
B1: the 16 Refinement production files live in Modules/Refinement/Domain and /Application, namespace equal to folder, nothing of the moving set is left under Services, the rename-list strings and Program.cs full names follow, and every rule stays green.
```

#### Promises

P1

P2

P4

#### Depends on

None.

#### Slice work

S

```text
Refinement is packaged in its module and every rule and registration follows it
```

##### Rework

None.

### S2

```text
B2: the 11 Refinement unit tests live in Tests/Modules/Refinement/Domain and /Application, namespace equal to folder, and the acceptance, wire-contract, model and usage-data tests stay.
```

#### Promises

P3

P4

#### Depends on

S1

#### Slice work

S

```text
The Refinement unit tests sit beside the module they test
```

##### Rework

None.
