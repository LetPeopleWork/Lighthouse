# Acceptance brief: story\-6365\-s03g\-b\-move

## Slices

### S1

##### Covers

P1

```text
Read the compiled backend and the source tree against the membership map of the Refinement module
```

```text
Each moving type exists once, only in the Domain (NeedBand) or Application namespace of Modules.Refinement, its file sits in the folder of that namespace declaring it, and no moving file remains under the old Services folders
```

```text
Refinement is packaged in its module: nothing of it is left half moved in Services
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs\:\:EveryMovingRefinementType\_IsFoundInItsNewModuleNamespaceOnly

```text
A type copied into the module but kept in the old namespace, an Application type declared in Domain, a file in the wrong module folder or declaring the old namespace, or a file left under Services would still pass this one test; the sibling file and old-folder tests and the made-up cases fail on each
```

###### Obligations







##### Covers

P1

```text
Read the source tree for the files of the moving set
```

```text
Each file sits in Modules/Refinement/Domain or /Application declaring that namespace and its listed types, and none remains under Services/Implementation/Refinement or Services/Interfaces/Refinement
```

```text
No half-moved Refinement file is left in Services
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs\:\:EveryMovingRefinementFile\_SitsInTheFolderOfItsNewNamespace\_WithTheTypesTheMapListsForIt

```text
A type declared in the right namespace by a file in the wrong folder passes the namespace test but fails here; a leftover old file fails the old-folder test
```

###### Obligations


##### Covers

P1

```text
Read the compiled backend for the Refinement types the map lists as staying
```

```text
Sizing-log repository pair, shared models, controllers, validator, DTOs and WeeklyRecurrence are each found once in the namespace the map lists
```

```text
The stored model and the API wire are untouched by the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs\:\:EveryRefinementTypeThatStays\_IsStillWhereTheMapListsIt

```text
A staying type moved while a copy remains in its old place would pass; the move owner removes the old file, and a type moved away fails by name, shown by the made-up case
```

###### Obligations


##### Covers

P2

```text
Read the list of strings the move renames
```

```text
The list holds no entry, because every string was renamed to the new place or removed
```

```text
No guard test still names the old Services path of a moved Refinement type
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs\:\:TheRenameList\_HoldsNoEntry\_BecauseTheMoveRenamesThemAll

```text
A string left unrenamed in a test file while its entry was deleted from the list would still pass; the guard tests that read those strings fail on it instead
```

###### Obligations


##### Covers

P1

P2

```text
Feed the checks a made-up finished move and made-up broken ones
```

```text
The finished move has no problem; each broken one (left, copied, wrong layer, wrong folder, stale namespace, old file, moved staying type, leftover entry) is named by its type or file and nothing else
```

```text
The checks fail on a half-done move and pass a finished one
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementLivesInItsModuleArchUnitTest\.cs\:\:TheCodeAfterTheMove\_HasNoProblemInAnyCheck

```text
A check that always passes or always fails is caught by the pairing of the finished case with the broken cases
```

###### Obligations


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
