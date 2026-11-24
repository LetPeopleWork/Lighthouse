# Acceptance brief: story\-6365\-s03g\-c\-edges\-and\-rules

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Searching the test project for the Refinement path fragments Services.Implementation.Refinement, Services.Interfaces.Refinement, Services/Implementation/Refinement and Services/Interfaces/Refinement finds no rule constant or string literal that names Refinement that way; Refinement is named only as Modules.Refinement.
```

#### E2

##### Promises

P1

```text
architecture-rules run on the candidate reports every existing architecture rule (RefinementPattern, Tracker integration, Access and Licensing lists, per-type patterns of RefinementModuleArchUnitTest) with the same verdict as on today code, with no rule newly failing and none removed.
```

#### E3

##### Promises

P2

```text
architecture-rules Refinement reports a Refinement rule set R-R1 to R-R6 whose listed types sit in their stated layers, Domain reaches only the kernel and BCL, outside code reaches Domain only from API, the composition root and Refinement Application, and Application reaches no EF Core, Data, API, module Adapters, Tracker integration or update code, with the Access, Organisation set-up, Platform lane and repository reach limited to the measured list.
```

#### E4

##### Promises

P2

```text
backend-tests Refinement shows that each made-up offender for each R-R rule and each list position fails the rule, and the failure message names the offending edge in the Refinement wording.
```

#### E5

##### Promises

P3

```text
ef-model-unchanged reports the database model identical to the model before feature C, so nothing the database sees changes.
```

#### E6

##### Promises

P3

```text
The whole filtered backend-tests run finishes with zero failures, and the candidate loosens no rule or list relative to the prior rule files.
```

## Charter

None.

## Examination

None.
