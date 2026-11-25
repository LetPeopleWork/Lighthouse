# Acceptance brief: story\-6366\-s03h\-b\-move

## Slices

### S1

##### Covers

P1

```text
Run the Reports placement architecture test over the compiled backend and the source tree
```

```text
The 36 types declared by the 31 moving files are each found only in their new Reports module namespace and folder, no moving file is left under the old Services folders, and the staying types are where the map lists them
```

```text
Reports sits in its module and nothing of it is left under Services
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsLivesInItsModuleArchUnitTest\.cs

```text
A behaviour change inside a moved file, which the unchanged acceptance and integration suites observe and not this test
```

###### Obligations


##### Covers

P2

```text
Run the same test, which reads the list of strings that name Reports by its old Services path
```

```text
The list holds no entry once every string is renamed or removed
```

```text
No test or rule still points at the old Reports folders
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsLivesInItsModuleArchUnitTest\.cs

```text
A string renamed to a wrong new name while its entry is removed; the build and the rules catch it, not this test
```

###### Obligations


##### Covers

P4

```text
Run the same test against the staying Reports types; the Reports acceptance, integration and wire suites run unchanged and are the observers of behaviour
```

```text
ReportRepository, the stored models, controllers, owner-write access and DTOs stay in their namespaces; this test is only the staying-types proxy, while the unchanged Reports acceptance, integration and wire suites observe that routes, answers and the stored model are unchanged
```

```text
Nothing an API client or the database sees changes
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsLivesInItsModuleArchUnitTest\.cs

```text
A behaviour change inside a moved file, which the unchanged acceptance, integration and wire suites observe and not this test
```

###### Obligations


### S2

##### Covers

P3

```text
Run the Reports unit-test placement check over the compiled test assembly and the source tree
```

```text
Each of the 18 Reports unit tests is found by name in its layer namespace (2 Domain, 5 Metrics, 11 Application) and in the folder of that namespace; the old folder is empty; the module test folder holds only those 18; the port, host, wire-contract and repository tests stay where they are, outside the module namespace
```

```text
The Reports unit tests sit beside the module they test, and the other Reports tests are untouched
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsUnitTestsBesideTheModuleArchUnitTest\.cs

```text
A class kept under the old folder or namespace while a copy sits in the module is not accepted; a test moved into the wrong layer, renamed, missing or a staying test moved is not accepted
```

###### Obligations



##### Covers

P4

```text
Run the same placement check, which only inspects where tests live, not their assertions
```

```text
No staying acceptance, port, host, wire-contract or repository test is moved or lost, so their unchanged assertions keep running
```

```text
Reports behaviour seen by users and clients is untouched
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsUnitTestsBesideTheModuleArchUnitTest\.cs

```text
Behaviour changes are not seen by this check; the unchanged Reports acceptance and wire tests and the full suite observe that
```

###### Obligations


##### Covers

P4

```text
Run the placement check of the 31 moved Reports production files (feature slice S1's oracle, preserved)
```

```text
Production types sit in their module layer namespaces and folders, old production folders hold no moving file, the rename list is empty
```

```text
Reports code lives in its module and behaviour is untouched
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsLivesInItsModuleArchUnitTest\.cs

```text
Pure placement: behaviour changes are seen by the unchanged acceptance and wire tests, not here
```

###### Obligations







## Expectations

### Rows

#### E1

##### Promises

P1

```text
Architecture rules probe with the Reports filter stays green after the move, so the layering rules that govern Reports still hold for a user of the build. Exact namespace and folder placement of each moved type is not shown by any admitted probe and is left to the whole-candidate review.
```

#### E2

##### Promises

P1

P3

```text
test-selection-show for a path under Lighthouse.Backend/Lighthouse.Backend/Modules/Reports/ and for a path under Lighthouse.Backend/Lighthouse.Backend.Tests/Modules/Reports/ prints backend: true and no live tracker suite (CI runs the backend suite for a change in the module); and backend-tests with the fragment Lighthouse.Backend.Tests.Modules.Reports runs the moved Reports unit tests by name, all passing. Leave namespace-equals-folder and exact file locations to architecture-rules (the layout tests name them) and to the whole-candidate review.
```

#### E3

##### Promises

P2

P4

```text
backend-tests with the fragment Reports runs the Reports acceptance, integration and wire-contract classes with their assertions unchanged (create, edit, read Then and Now, list across owners, bulk create, bulk removal, cap, refusals). It takes longer than the 2-minute foreground shell limit, so it runs as a background shell job and is read when it ends. The rename list, Program.cs registration order and the lifetimes are not shown by this probe and are left to the whole-candidate review.
```

#### E4

##### Promises

P4

```text
ef-model-unchanged shows the EF model is unchanged with no migration added by the move, so a user of the database sees no schema change.
```

#### E5

##### Promises

P2

P4

```text
architecture-rules and the whole filtered backend suite (backend-tests without fragment, run as a background shell job since it takes about 7 minutes) stay green after the move, so no user-visible behaviour regresses anywhere in the backend. Routes, DTOs and headers being unchanged is not shown by these probes and is left to the whole-candidate review.
```

## Charter

None.

## Examination

None.
