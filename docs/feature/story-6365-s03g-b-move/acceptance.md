# Acceptance brief: story\-6365\-s03g\-b\-move

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Each type in the Refinement membership map is found only in its new Refinement Domain or Application namespace and folder; none remains in the old Refinement service folders, and the staying Refinement files stay where they were. Observable as: architecture-rules reports no violation for the moved set and the staying set.
```

#### E2

##### Promises

P2

```text
The Refinement registrations in Program.cs keep the same order, lifetimes and service-to-implementation pairs, changing only full type names; every architecture rule and the preservation law stay green after the move.
```

#### E3

##### Promises

P3

```text
The 11 Refinement unit tests sit under Modules/Refinement/Domain (NeedBand) and Modules/Refinement/Application (the other 10), each with namespace equal to folder; acceptance, wire-contract, model, usage-data and SizingLogRepository tests stay in place with only using lines changed. Observable as: backend-tests runs them under the expected fragments and test-selection-show places them correctly.
```

#### E4

##### Promises

P4

```text
Refinement acceptance and wire-contract tests pass with unchanged assertions for tab, need, votes, comments, take-back and settings, and the whole filtered backend suite stays green. Observable as: backend-tests on the Refinement fragment and on the filtered suite is green.
```

#### E5

##### Promises

P4

```text
The EF model is unchanged with no migration, routes, DTOs and headers are unchanged, and CI selects the backend suite for a change under Modules/Refinement. Observable as: ef-model-unchanged reports no model difference and test-selection-show for a Modules/Refinement path selects the backend suite.
```

## Charter

None.

## Examination

None.
