# Product brief: story\-6365\-s03g\-c\-edges\-and\-rules

## Request

```text
ADO Story #6365 S03g Package Refinement, feature C (edges and rules): every Refinement rule names Refinement by Modules.Refinement only, and no rule constant or string literal in the test project still matches the old Services.Implementation.Refinement or Services.Interfaces.Refinement namespaces or paths (C1); Refinement gets its own module rule set on ModuleRules, read from the IL with baselines measured there and lists inline by full type name, compared both ways and never loosened: R-R1 each listed type in its layer and none under Services, R-R2 Domain reaches only the kernel and the BCL, R-R3 outside reaches Domain only from API, the composition root and Refinement Application, R-R4 outside reaches Application only from API and the composition root, R-R5 Application reaches no EF Core, Data, API, module Adapters, Tracker integration or update code, R-R6 Application reaches Access, Organisation set-up, the Platform lane and repositories only along a measured list (C2). Test side only. DESIGN: docs/feature/story-6365-s03g-package-refinement/feature-delta.md; features A and B: docs/feature/story-6365-s03g-a-rules-first/ and docs/feature/story-6365-s03g-b-move/.
```

## Work

S

```text
Refinement's boundaries are guarded by its own module rules and no rule still points at where it used to live
```

### Rework

None.

## Promises

### P1

Outcome

```text
No rule constant and no string literal in the test project matches Services.Implementation.Refinement, Services.Interfaces.Refinement, Services/Implementation/Refinement or Services/Interfaces/Refinement any more: RefinementPattern, the Tracker integration, Access and Licensing lists and the per-type patterns of RefinementModuleArchUnitTest name Refinement by Modules.Refinement only, and every rule keeps its verdict on today's code.
```

### P2

Outcome

```text
Refinement has its own module rule set on ModuleRules, read from the compiled IL, with every list inline by full type name, compared both ways and never loosened: each listed type sits in its layer and none under Services (R-R1); Domain reaches only the kernel and the BCL (R-R2); code outside Refinement reaches Domain only from API, the composition root and Refinement Application (R-R3) and Application only from API and the composition root (R-R4); Application reaches no EF Core, Data, API, module Adapters, Tracker integration or update code (R-R5), and reaches Access, Organisation set-up, the Platform lane and repositories only along a list measured from the IL (R-R6). A made-up offender for each rule and each list position makes the rule fail and the failure names the edge in Refinement's own wording.
```

### P3

Outcome

```text
Nothing a user, an API client, a tracker or the database sees changes: feature C edits only test-side rule files (no production file, no Program.cs, no configuration key), no rule or list is loosened, and the whole filtered backend suite stays green.
```

## Scope

```text
Narrowing every Refinement rule constant and the per-type patterns to Modules.Refinement only, and settling feature A's preservation law and old-path sweep for Refinement now that the move is done; the RefinementModuleBoundariesArchUnitTest rule set R-R1 to R-R6 on ModuleRules with IL-measured baselines and made-up offenders.
```

```text
Any production file or Program.cs change; a separate Refinement unit-test placement test (feature B's RefinementUnitTestsBesideTheModuleArchUnitTest already pins it); the architecture documents ARCHITECTURE.md section 4, ADR-270 and the Epic catalogue row, which the finalize checklist updates before the push; the sizing-log repository pair and Models/Refinement (stay).
```

## Decisions

```text
Rigor standard for feature C: architect, acceptance designer and crafter on sonnet; reviewers and EXAMINE on haiku (DESIGN open question 1, AFK default taken 2026-11-24).
```

#### Status

Confirmed


```text
The Refinement unit-test placement test the DESIGN record names for S3 is not written again: feature B's RefinementUnitTestsBesideTheModuleArchUnitTest already pins the 11 unit tests to their layer folders and the staying tests in place. The architecture documents are prose and go into the finalize checklist, because nwdes accepts prose-only slices only under nWave's own folders.
```

#### Status

Confirmed


```text
Each slice's RED: C1 fails while any rule constant or literal still matches the old Refinement namespaces or paths (the old-namespace alternatives features A and B left in place); C2 fails on a made-up offender per rule and per list position. Behaviour is proved by the unchanged tests and EXAMINE through admitted probes (architecture-rules Refinement and all, backend-tests, ef-model-unchanged). Behaviour-free: no review window, UI and usage-data N/A, Quint N/A.
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
C1: no rule constant or string literal in the test project matches the old Refinement namespaces or paths any more; every Refinement rule names Modules.Refinement only and keeps its verdict.
```

#### Promises

P1

P3

#### Depends on

None.

#### Slice work

S

```text
No rule still points at where Refinement used to live
```

##### Rework

None.

### S2

```text
C2: Refinement has its own module rules R-R1 to R-R6 on ModuleRules, read from the IL with measured inline lists, and each catches a made-up offender in Refinement's own wording.
```

#### Promises

P2

P3

#### Depends on

S1

#### Slice work

S

```text
A new edge into or out of Refinement is caught by its own rules
```

##### Rework

None.
