# Acceptance brief: story\-6366\-s03h\-b\-move

## Slices

### S1

##### Covers

P1

```text
Run the Reports placement architecture test over the compiled backend and the source tree
```

```text
Each of the 31 moving types is found only in its new Reports module namespace and folder, no moving file is left under the old Services folders, and the staying types are where the map lists them
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
Run the same test against the staying Reports types
```

```text
ReportRepository, the stored models, controllers, owner-write access and DTOs stay in their namespaces, so the wire and stored model are unchanged
```

```text
Nothing an API client or the database sees changes
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsLivesInItsModuleArchUnitTest\.cs

```text
A behaviour change inside a moved file, which the unchanged acceptance and integration suites observe and not this test
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
