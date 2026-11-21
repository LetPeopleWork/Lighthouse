# Product brief: story\-6363\-s03e\-c\-edges\-and\-rules

## Request

```text
ADO Story #6363 S03e Package Access and Licensing, feature C (edges and rules): Access has its own module rules with inline lists measured from the IL (C1); Licensing has its own module rules with every reader listed, ForecastUpdater included, and the neighbour lists re-keyed (C2); every Access and Licensing unit test sits under Tests/Modules, no file of either context is left in Services/{Interfaces,Implementation}, and RbacIdentityPattern is retired (C3). Rules only: production behaviour unchanged. DESIGN and DEVOPS: docs/feature/story-6363-s03e-package-access-licensing/feature-delta.md; features A (rules first) and B (the move, pushed 19d131189) are delivered.
```

## Work

M

```text
Access and Licensing each have module rules that fail on a new offender, and their tests sit with their modules
```

### Rework

None.

## Promises

### P1

Outcome

```text
Access has its own module rules: each listed Access type lives in its layer (Domain, Application, Adapters/Http) and none is left under the old Services Auth or Authorization namespaces; its Domain reaches only the shared kernel (Models) and the BCL; outsiders reach its Domain and Application only from API, the composition root or as listed, and its Adapters/Http only from API and the composition root; its Application reaches no API and no module Adapters, reaches Data and platform types only as listed, and Access reaches no other context. Every list is inline, by full type name, measured from the compiled code and compared both ways, and a made-up offender against each list position, including one that reaches only through an attribute, fails a named rule.
```

### P2

Outcome

```text
Licensing has its own module rules: its types live in Application and Adapters/Http and none is left under the old Services Licensing namespaces; it reaches no Access type and no other context; outsiders reach its Application only from API, the composition root, Access Application or as listed, with every reader listed, ForecastUpdater included, and its Adapters/Http only from API; its Application reaches platform types only as listed. Every list is measured from the compiled code and compared both ways, a made-up offender against each rule fails it, and the Forecasting and Work Items lists that name Access or Licensing types keep those entries under the new names.
```

### P3

Outcome

```text
Every Access and Licensing unit test sits under Tests/Modules/Access or Tests/Modules/Licensing, no file of either context is left in Services/Interfaces or Services/Implementation (production or tests) with no per-file exception for the old Licensing test folder, and RbacIdentityPattern is retired with its one assertion carried by the new module rules; a made-up file in an old folder is caught.
```

### P4

Outcome

```text
Rules only: nothing a user, a tracker, the database or the licence check sees changes. Every route, guard, authentication scheme, RBAC answer and licence answer stays the same, the existing RBAC rules (EveryActionDeclaresItsAccess, AccessTablesSingleReader, LicenseGateSingleSource, the Reports and Refinement guard rules) keep every assertion, and every existing test stays green.
```

## Scope

```text
Architecture rule files and their inline lists in Lighthouse.Backend.Tests/Architecture: two new module-boundary rule files for Access and Licensing, the re-keyed neighbour lists, the unit-test placement and no-file-left check, and the retirement of RbacIdentityPattern.
```

```text
Any production code change; the candidate 2 readers of Licensing (owners Epic 6474 and #6657), which stay listed rather than removed; Access persistence (entities, repositories, seeders), which stays where it is and is a catalogue note only; the finalize work (mutation on frozen code, ADR-270, ARCHITECTURE.md section 4, brief ACs, catalogue, the Lighthouse finalize checklist), which is not an nwdes slice.
```

## Decisions

```text
Rigor thorough for all of feature C: architect, acceptance designer and crafter on opus; reviewers and EXAMINE on sonnet (maintainer, 2026-11-17).
```

#### Status

Confirmed


```text
C3's placement check has no per-file exception for the old Licensing test folder: B4 moved the Licensing test data (TestLicenseData, the licence files, LicensingIntegrationTest) to Tests/Modules/Licensing (maintainer, 2026-11-17, answer 2).
```

#### Status

Confirmed


```text
The Forecasting and Work Items lists that name Access or Licensing types keep their entries under the new names; they are those modules' outbound edges and shrink only with candidate 2. Licensing readers stay listed, ForecastUpdater newly listed (DESIGN record, review approved 2026-11-17).
```

#### Status

Confirmed


```text
The existing RBAC rules (EveryActionDeclaresItsAccess, AccessTablesSingleReader, LicenseGateSingleSource, the Reports and Refinement guard rules) keep every assertion; RbacIdentityPattern is retired in C3 when nothing old matches, its one assertion moving to the new module rule files (DESIGN record, review approved 2026-11-17).
```

#### Status

Confirmed


```text
Does C3 have a RED? Measured 2026-11-21: the old Services/{Implementation,Interfaces}/{Auth,Authorization,Licensing} folders hold 0 files in production and tests, so the placement and no-file-left check will likely pass on arrival, as S03d's C3 did (dropped there as an nwdes slice and kept as a regression guard). The retirement of RbacIdentityPattern is a real edit. The acceptance designer decides whether C3's oracle has an observable RED (for example a made-up file in an old folder, or the retired pattern still being read) or whether C3 folds into C2 as a regression guard.
```

#### Status

Open

Acceptance

## Journey

None.

## Slices

### S1

```text
C1: Access has its own module rules (Architecture/AccessModuleBoundariesArchUnitTest.cs) with inline lists measured from the IL; a made-up offender per list position, including one reaching only through an attribute, fails a named rule.
```

#### Promises

P1

P4

#### Depends on

None.

#### Slice work

M

```text
Access fails a named rule on a new offender across its border
```

##### Rework

None.

### S2

```text
C2: Licensing has its own module rules (Architecture/LicensingModuleBoundariesArchUnitTest.cs) with every reader listed, ForecastUpdater included, and the neighbour lists re-keyed; a made-up offender against each rule fails it.
```

#### Promises

P2

P4

#### Depends on

None.

#### Slice work

S

```text
Licensing fails a named rule on a new offender across its border
```

##### Rework

None.

### S3

```text
C3: every Access and Licensing unit test sits under Tests/Modules, no file of either context is left in Services/{Interfaces,Implementation}, and RbacIdentityPattern is retired; a made-up file in an old folder is caught.
```

#### Promises

P3

P4

#### Depends on

S1

S2

#### Slice work

S

```text
Access and Licensing tests sit with their modules, and the old shared pattern is gone
```

##### Rework

None.
