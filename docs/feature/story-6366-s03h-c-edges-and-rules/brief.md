# Product brief: story\-6366\-s03h\-c\-edges\-and\-rules

## Request

```text
ADO Story #6366 S03h Package Reports, feature C (edges and rules): every Reports rule names Reports by Modules.Reports only, the joint Tracker integration pattern drops Reports and its dead Auth, Authorization and Licensing alternatives, and no rule constant or string literal in the test project still matches the old Services.Implementation.Reports or Services.Interfaces.Reports namespaces or paths (C1); Reports gets its own module rule set ReportsModuleBoundariesArchUnitTest on ModuleRules, read from the IL with baselines measured there and lists inline by full type name, compared both ways and never loosened: R-RP1 each listed type in its layer and none under Services, R-RP2 Domain reaches only the kernel and the BCL, R-RP3 outside reaches Domain only from API, the composition root and Reports Application, R-RP4 outside reaches Application only from API, the composition root and ReportRepository, R-RP5 Application reaches no Data, API, module Adapters, Tracker integration, RBAC or update code and EF Core only along a measured list, R-RP6 Application reaches Flow metrics Domain, Licensing, Organisation set-up data and platform infrastructure only along a measured list (C2). Test side only. DESIGN: docs/feature/story-6366-s03h-package-reports/feature-delta.md; features A and B: docs/feature/story-6366-s03h-a-rules-first/ and docs/feature/story-6366-s03h-b-move/.
```

## Work

S

```text
Reports' boundaries are guarded by its own module rules and no rule still points at where it used to live
```

### Rework

None.

## Promises

### P1

Outcome

```text
No rule constant and no string literal in the test project matches Services.Implementation.Reports, Services.Interfaces.Reports, Services/Implementation/Reports or Services/Interfaces/Reports any more, outside feature A's measurement files: ReportsPattern names Reports by Modules.Reports only, the joint Tracker integration pattern RefinementReportsAccessAndLicensing no longer names Reports nor the dead Auth, Authorization and Licensing alternatives, the Access and Licensing RefinementAndReports patterns name Reports by Modules.Reports only, and every rule keeps its verdict on today's code; ReportsModuleArchUnitTest keeps every assertion.
```

### P2

Outcome

```text
Reports has its own module rule set on ModuleRules, read from the compiled IL, with every list inline by full type name, compared both ways and never loosened: each listed type sits in its layer and none under Services (R-RP1); Domain reaches only the kernel and the BCL (R-RP2); code outside Reports reaches Domain only from API, the composition root and Reports Application (R-RP3) and Application only from API, the composition root and ReportRepository (R-RP4); Application reaches no Data, API, module Adapters, Tracker integration, RBAC or update code, and EF Core only along a measured list (R-RP5); Application reaches Flow metrics Domain, Licensing, Organisation set-up data and platform infrastructure only along a list measured from the IL (R-RP6). A made-up offender for each rule and each list position makes the rule fail and the failure names the edge in Reports' own wording.
```

### P3

Outcome

```text
Nothing a user, an API client, a tracker or the database sees changes: feature C edits only test-side rule files (no production file, no Program.cs, no configuration key), no rule or list is loosened, and the whole filtered backend suite stays green.
```

## Scope

```text
Narrowing every Reports rule constant to Modules.Reports only and removing Reports and the dead Auth, Authorization and Licensing alternatives from the joint Tracker integration pattern; settling feature A's preservation law and old-path sweep for Reports now that the move is done; the ReportsModuleBoundariesArchUnitTest rule set R-RP1 to R-RP6 on ModuleRules with IL-measured baselines and made-up offenders.
```

```text
Any production file or Program.cs change; a separate Reports unit-test placement test (feature B's ReportsUnitTestsBesideTheModuleArchUnitTest already pins it); the architecture documents ARCHITECTURE.md section 4, ADR-270 and the Epic catalogue row, which the finalize checklist updates before the push; ReportRepository and Models/Reports (stay).
```

## Decisions

```text
Rigor standard for feature C: architect, acceptance designer and crafter on sonnet; reviewers and EXAMINE on haiku (DESIGN open question 1, maintainer's take-DESIGN-defaults on 2026-11-24).
```

#### Status

Confirmed


```text
The Reports unit-test placement test the DESIGN names for S3 is not written again: feature B's ReportsUnitTestsBesideTheModuleArchUnitTest already pins the 18 unit tests to their layer folders and the staying tests in place. The architecture documents are prose and go into the finalize checklist, because nwdes accepts prose-only slices only under nWave's own folders. The dead Auth, Authorization and Licensing alternatives go now, as the host decided at feature A's start.
```

#### Status

Confirmed


```text
Each slice's RED: C1 fails while any rule constant or literal still matches the old Reports namespaces or paths (the old-namespace alternatives features A and B left in place); C2 fails on a made-up offender per rule and per list position. Behaviour is proved by the unchanged tests and EXAMINE through admitted probes (architecture-rules Reports and all, backend-tests as a background job, ef-model-unchanged). Behaviour-free: no review window, UI and usage-data N/A, Quint N/A.
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
C1: no rule constant or string literal in the test project matches the old Reports namespaces or paths any more; every Reports rule names Modules.Reports only and keeps its verdict, and the dead Auth, Authorization and Licensing alternatives of the joint Tracker integration pattern are gone.
```

#### Promises

P1

P3

#### Depends on

None.

#### Slice work

S

```text
No rule still points at where Reports used to live
```

##### Rework

None.

### S2

```text
C2: Reports has its own module rules R-RP1 to R-RP6 on ModuleRules, read from the IL with measured inline lists, and each catches a made-up offender in Reports' own wording.
```

#### Promises

P2

P3

#### Depends on

S1

#### Slice work

S

```text
A new edge into or out of Reports is caught by its own rules
```

##### Rework

None.
