# Acceptance brief: story\-6365\-s03g\-a\-rules\-first

## Slices

### S1

##### Covers

P1

P4

```text
The architecture check compares the Refinement membership list with the types the compiled code holds in the old Refinement namespaces and under Modules.Refinement
```

```text
On today's code all 41 listed moving types are found, each exactly once, in their old namespace, and the code holds no Refinement type the list leaves out; written before the list was filled, the check failed naming all 41
```

```text
The maintainer has a fixed, measured list of what moves into the Refinement module and to which layer, and nothing a user sees changes
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:\:TheRefinementTypesInTheCompiledCode\_AreExactlyTheTypesTheMapListsToMove\_EachFoundOnce

```text
A list entry names the right type with the wrong layer, as long as the type has not moved yet; the layer check below catches that
```

###### Obligations



##### Covers

P1

```text
The membership comparison is run on altered lists: one type left off, a made-up name added, a listed type taken out of the code, one type swapped for a made-up one, a type found in its new namespace, a type left behind beside its moved copy, a type moved into the other layer, a namesake in the other old namespace, and one type listed twice
```

```text
Each altered list fails and names exactly the type concerned, except the type found in its new namespace, which still matches
```

```text
A wrong or out-of-date membership list cannot pass unnoticed, and the move itself keeps the check green
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:\:AMapWithOneTypeLeftOff\_NamesThatTypeAsNotOnTheMap

```text
A made-up type in a Refinement namespace that the compiled code never holds; only the names the code actually holds are compared
```

###### Obligations




##### Covers

P1

```text
The collection is run on a made-up set of type names under the old and new Refinement namespaces, with nested, compiler-made and lookalike names among them
```

```text
Exactly the top-level types of the old namespaces and of every namespace under Modules.Refinement are collected
```

```text
A type added during the move joins the list check, and a lookalike namespace does not
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:\:TheCollection\_TakesEveryTopLevelTypeInTheOldAndTheNewNamespaces\_AndNothingElse

```text
A type the compiler makes under a name not starting with an angle bracket
```

###### Obligations


##### Covers

P1

```text
The layer check reads, per source file, what every type it declares reaches in the compiled code, and whether the file declares a port interface
```

```text
Only NeedBand.cs is Domain; every port file stays Application with all its records and enums; no file of the Domain reaches beyond the shared Models, the base library and the Domain; altered lists fail: SizingRefusal.cs as Domain is named with the logger it reaches, NeedBand.cs as Application is named, a port record listed as Domain or a wrong port-file mark is named, and a made-up Domain type reaching a lookalike of Models or System or a namesake of a Domain type is named while its kernel and Domain edges are not
```

```text
The move puts each file into the layer its dependencies allow, and the Domain stays free of services, ports and logging
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:\:EachFileIsDomain\_ExactlyWhenItIsNoPortFile\_AndEveryTypeItDeclaresReachesOnlyTheSharedModelsTheBaseLibraryAndTheDomain

```text
A port file whose interface name does not start with I is taken for an ordinary file
```

###### Obligations





##### Covers

P1

```text
Each of the 31 Refinement types that stay is looked up in the compiled code at its listed namespace; altered entries put one in the wrong namespace, give one a made-up name and list a moving type as staying
```

```text
All 31 are found where listed; each altered entry is named, and correct entries are not
```

```text
The maintainer sees who owns every Refinement type that stays and why
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:\:EachTypeThatStays\_IsInTheCompiledCodeWhereListed\_OutsideTheRefinementNamespaces

```text
A staying type with an inaccurate owner or reason text; those are read by review, not checked
```

###### Obligations


##### Covers

P1

```text
The suppression, analyser, CI-selection, test-budget, host-user and Stryker files are read for targets that name a moving type by its namespace or folder, old or new
```

```text
No target names a moving type today; a made-up target naming one by old name, new name, old path or its test's old name is named, one naming a staying type or a lookalike namespace is not, a listed target its file no longer holds is named, a listed target rewritten to the new namespace still matches, and a missing file the scan must read is named
```

```text
No warning suppression, test selection, budget or mutation run silently stops covering Refinement code when it moves
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementMembershipArchUnitTest\.cs\:\:TheTargetsNamingAMovingType\_AreExactlyTheOnesListed

```text
A target in a file outside the scanned set, such as an archived document
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
A single membership test for the Refinement module exists and passes when run through backend-tests (fragment naming it): it lists every moving Refinement type with layer, old and new namespace, and every staying Refinement file with owner and reason.
```

#### E2

##### Promises

P1

```text
Negative control: when a listed type name does not exist, or a listed type is found in neither its old nor its new namespace, the membership test fails; with the real code it does not.
```

#### E3

##### Promises

P1

```text
Layer rule: a type listed as Domain reaches nothing beyond the kernel, the BCL and other Refinement Domain types; architecture-rules with a Refinement fragment shows this rule present and passing, and a Domain type that reaches an outside type would fail it.
```

#### E4

##### Promises

P1

```text
The membership test also records, in a readable form, every suppression, analyser, CI-selection, test-budget, host-user and Stryker target that names a moving type; test-selection-show lists the CI selection entries and each moving type named there appears in that record.
```

#### E5

##### Promises

P2

```text
For each moving Refinement type, each architecture rule pattern in the test assembly gives the same verdict under the new Modules.Refinement namespace as under the old one; architecture-rules over the whole rule set passes, and a probe type in the new namespace is matched exactly where its old-namespace twin was.
```

#### E6

##### Promises

P2

```text
No added namespace alternative in a rule matches a production type outside Modules.Refinement: a type outside the module (for example another module's type) keeps the verdict it had before the rules were changed.
```

#### E7

##### Promises

P2

```text
The rules that used the exact old namespace (equality, DoNotResideInNamespace, the exact-namespace assertion) now read from the membership map and keep their verdict through the move; architecture-rules for those fragments passes, and removing a type from the map makes the dependent rule fail rather than pass silently.
```

#### E8

##### Promises

P3

```text
A check exists that fails when an unlisted rule constant or string literal in the test project names Refinement by Services.Implementation.Refinement, Services.Interfaces.Refinement or Services/Implementation/Refinement; with the real tree it passes through backend-tests.
```

#### E9

##### Promises

P3

```text
The fixed rename list is checked both ways: a listed literal that no longer exists fails the check, an unlisted new one fails it, and the list may only shrink compared with the pre-move search.
```

#### E10

##### Promises

P4

```text
ef-model-unchanged reports the EF model identical to before feature A, for every supported database provider (no migration, no schema or mapping change).
```

#### E11

##### Promises

P4

```text
No production file is moved, renamed or behaviourally changed by feature A: the only differences are in the test project; a Refinement type keeps its old namespace and the API (routes, DTOs, responses) is unchanged.
```

#### E12

##### Promises

P4

```text
Every existing backend test stays green (whole-suite run excluding live-connector categories), and test-selection-show still selects the same tests as before for the CI selection.
```

## Charter

None.

## Examination

None.
