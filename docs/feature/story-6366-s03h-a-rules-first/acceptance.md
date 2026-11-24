# Acceptance brief: story\-6366\-s03h\-a\-rules\-first

## Slices

### S1

##### Covers

P1

P4

```text
The Reports membership check runs over the compiled code: every top-level type in the old Reports namespaces and under Modules.Reports is compared with the moving types on the map, each listed with its file, layer and old namespace.
```

```text
On today's code all 36 listed moving types are found, each exactly once, in their old namespace, and the code holds no Reports type the map leaves out; written before the map was filled, the check failed naming all 36 types as not on the map.
```

```text
The maintainer has a fixed, measured list of the 36 Reports types that move, with the layer each goes to, and nothing a user sees changes.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs\:\:TheReportsTypesInTheCompiledCode\_AreExactlyTheTypesTheMapListsToMove\_EachFoundOnce

```text
A type listed in the wrong layer whose old namespace is still found passes this comparison; the layer rule judges it instead.
```

###### Obligations




##### Covers

P1

```text
The membership comparison is run on altered lists and altered code: one type left off, a made-up name added, a listed type taken out of the code, one type swapped for a made-up one, a type found in its new namespace, a type left behind beside its moved copy, a type moved into another layer, a namesake in another old namespace, and one type listed twice.
```

```text
Each altered case fails and names exactly the type concerned under the right heading (not on the map, not in the code, found twice, listed twice), except the type found in its new namespace, which still matches.
```

```text
A change to the Reports code or to the map that breaks the membership is named type by type, so the maintainer knows which entry to fix.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs\:\:AMapWithOneTypeLeftOff\_NamesThatTypeAsNotOnTheMap

```text
A map entry with the right name and namespace but a wrong file name passes the comparison; the layer rule groups by file and review reads the file names.
```

###### Obligations




##### Covers

P1

```text
The collection of Reports types is run over a made-up list of names in the old namespaces, the Metrics namespace, Modules.Reports and its layers, nested and compiler-made names, lookalike namespaces, a staying model and a test namespace.
```

```text
It takes exactly the top-level types of the old Reports namespaces and of every namespace under Modules.Reports, a nested type as its holder, and leaves out compiler-made names, lookalikes, the staying model and the test namespace.
```

```text
No Reports type can join or leave the module unseen during the move, and a lookalike does not join it by mistake.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs\:\:TheCollection\_TakesEveryTopLevelTypeInTheOldAndTheNewNamespaces\_AndNothingElse

```text
A collection that reads only the sub-namespaces named here would still pass; a sub-namespace nobody named yet is covered only by the open pattern.
```

###### Obligations


##### Covers

P1

```text
The layer of every moving file is checked against what its types reach in the compiled code, and the check is run on altered lists: DailySeriesSummary listed as Domain, ReportCapPolicy listed as Application, a Metrics file listed as Application and another file as Application Metrics, a port record listed as Domain and a non-port file marked as a port file, and a made-up Domain type reaching a lookalike of Models or System, a namesake of a Domain type and a made-up fixture type of the same name.
```

```text
On today's code exactly ReportCapPolicy, ReportSeriesReading and ReportWindowPolicy are Domain, the 11 port files are Application with all their records, the 8 other Metrics files are Application Metrics and the rest Application; each altered list fails naming its file with the reason (DailySeriesSummary with the Flow metrics Domain type it reaches), and correct files, the kernel, the base library and the Domain itself are not named.
```

```text
The Domain of Reports holds only rules that depend on the shared Models and the base library, and a file that could be Domain is not hidden in the Application.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs\:\:EachFileIsDomain\_ExactlyWhenItIsNoPortFile\_AndEveryTypeItDeclaresReachesOnlyTheSharedModelsTheBaseLibraryAndTheDomain

```text
A reach the compiled code does not show, such as a reference only in a catch clause, an event or an attribute argument, passes as kernel-only.
```

###### Obligations






##### Covers

P1

```text
Each listed staying Reports type (ReportRepository, every Models.Reports type, the three controllers, the controller base, the owner write access and the seven request types) is looked up in the compiled code at its listed namespace, and the check is run with one entry in the wrong namespace, one under a made-up name, a moving type listed as staying and a Models.Reports type left off.
```

```text
All listed staying types are found where listed and every type of Models.Reports and of the Reports requests is listed; each altered entry is named and correct entries are not.
```

```text
Every Reports type that does not move is named with its owner and why it stays.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs\:\:EachTypeThatStays\_IsInTheCompiledCodeWhereListed\_AndEveryTypeOfTheStayingReportsNamespacesIsListed

```text
A wrong owner or reason text passes; review judges those. A staying type outside the Reports-named namespaces that nobody listed is not found by the completeness part.
```

###### Obligations


##### Covers

P1

```text
The suppression, analyser, CI-selection, test-budget, host-user, Stryker and log-override files are read for targets naming a moving Reports type by its old or new name, path or test name, and compared with the listed targets; the check is run with a made-up file of such targets, with a listed target its file no longer holds, with one rewritten to the new namespace, and with a scan file missing.
```

```text
Today no such target exists and none is listed; the made-up targets naming a moving type are named and those naming staying types, staying tests or a lookalike are not; the vanished listed target is named, the rewritten one still matches, and the missing scan file is named.
```

```text
No warning suppression, test selection, test budget, mutation run or log level silently stops applying when Reports moves.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs\:\:TheTargetsNamingAMovingType\_AreExactlyTheOnesListed

```text
A target built at run time from fragments, or one in a file kind outside the scanned set, passes unseen.
```

###### Obligations



##### Covers

P4

```text
The whole architecture rule suite and the backend build run with the new membership check added and no production file changed.
```

```text
The build has no warning and every architecture rule passes; only two test-side files are added.
```

```text
Nothing a user, a tracker or the database sees changes.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture \(FullyQualifiedName\~Lighthouse\.Backend\.Tests\.Architecture\)

```text
A production change outside what the architecture rules read passes here; the host's full filtered backend run and the empty production diff carry that.
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Observing the compiled test assembly through the architecture-rules probe, the Reports module membership list shows each moving Reports type with its layer (Domain, Application or Application/Metrics) and its old and new namespace, and each staying Reports file with an owner and reason; every listed type is found under exactly its old or new namespace, and no listed name is absent.
```

#### E2

##### Promises

P1

```text
A Domain type in the Reports membership list depends on nothing beyond the kernel and the base library; the probe shows no Domain Reports type referencing any other assembly or namespace, and the membership test fails if a listed type is missing or a listed name does not exist.
```

#### E3

##### Promises

P2

```text
For each moving Reports type, every architecture rule pattern in the test assembly gives the same match answer under the new Modules.Reports namespace as under the old namespace, as shown by architecture-rules probes run against Reports class fragments.
```

#### E4

##### Promises

P2

```text
No alternative added to a rule pattern matches any production type outside Modules.Reports; probing the rules with non-Reports production class fragments shows no new match.
```

#### E5

##### Promises

P3

```text
No rule constant or string literal in the test project names Reports by the old Services.Implementation.Reports, Services.Interfaces.Reports or Services/(Implementation|Interfaces)/Reports path, except entries of a fixed rename list; the check fails when a listed literal no longer exists or an unlisted one appears, and the list can only shrink.
```

#### E6

##### Promises

P4

```text
Nothing a user, a tracker or the database sees changes: the ef-model-unchanged probe reports the EF model identical to before, and no production file moves or changes behaviour in feature A.
```

#### E7

##### Promises

P4

```text
Every existing backend test stays green after the change, as seen through the backend-tests and test-selection-show probes.
```

## Charter

None.

## Examination

None.
