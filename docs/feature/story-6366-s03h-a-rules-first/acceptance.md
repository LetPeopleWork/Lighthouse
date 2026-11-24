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

### S2

##### Covers

P2

```text
Every constant and static read-only rule pattern in the test assembly is asked, on the subject its rule reads, whether it sees each moving Reports type under the type's old namespace and under its new Modules.Reports namespace.
```

```text
No pattern breaks its role: a pattern for the Reports context sees each moving type in both places, an old location or a pattern that saw Reports only by its folder does not follow it, and no undeclared pattern sees a moving type. Before the Reports rule constants are widened this names ReportsPattern, RefinementReportsAccessAndLicensing, both RefinementAndReports constants and the readers built from them as stopping to see the type.
```

```text
Once the Reports files move, every architecture rule that guards or forbids Reports still judges them, so no rule quietly stops checking.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs\:\:EveryRulePatternThatSeesAMovingReportsType\_IsDeclared\_AndKeepsItsRoleOnTheSubjectItsRuleReads

```text
A rule that names Reports only by a string built at run time from fragments that never appear as one constant, or a pattern declared folder-only by mistake so it may lose Reports
```

###### Obligations







##### Covers

P2

```text
The same role check is run over made-up rule constants, each declared with a subject and a role: a context that sees only the old place, one that names Reports as one choice of a group, ones widened by type names or only to the Application namespace, an old location widened into the new place, a part of the code that never saw Reports given Modules.Reports, a folder-only match that follows the types, undeclared constants in a nested class, and an interpolated reader built from a blind context.
```

```text
The check names exactly those made-up constants with their break kind, passes the ones that keep their role (both places named, an anchored module alternative on a group, a full-name pattern widened by type names, an old location left in place), and the selection widened only to Application loses exactly the Domain and Metrics types.
```

```text
The check cannot be fooled into passing a rule that would go blind or wrong at the move.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs\:\:TheRoleCheck\_NamesEachMadeUpPatternThatBreaksItsRole\_AndPassesTheOnesThatKeepIt

```text
A break kind the made-up set does not stage, such as a composite whose component is declared as an old location
```

###### Obligations








##### Covers

P2

```text
Made-up patterns over all of Modules, one on the whole-parent list, one listed but narrower than Modules, one not listed, are put through the role check.
```

```text
Only the listed pattern over all of Modules may see the moved Reports types for the first time; the narrower listed one and the unlisted one are named.
```

```text
A pattern over every module may take Reports in once it becomes a module, and no other pattern may start judging the moved types unnoticed.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs\:\:TheWholeParentList\_AdmitsTheNewSightOfAListedPatternOverAllOfModules\_AndNoOther

```text
A whole-parent pattern that takes in all of Modules but is not meant to see Reports
```

###### Obligations


##### Covers

P2

```text
Every Modules.Reports alternative of a rule pattern in the test assembly is asked about every production namespace and full name outside Modules.Reports, on the subject the pattern is declared with.
```

```text
No alternative matches anything outside Modules.Reports.
```

```text
A rule widened for the move takes in only the moved Reports types and nothing it was never about.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs\:\:NoModulesReportsAlternativeOfARulePattern\_MatchesAProductionNamespaceOrTypeOutsideModulesReports

```text
An unanchored alternative that today matches nothing outside but would match a namespace added later
```

###### Obligations



##### Covers

P2

```text
The alternative check is asked over made-up keys about the production names plus a lookalike namespace that only starts like Modules.Reports.
```

```text
Only the made-up key with an unanchored Modules.Reports alternative is named; the anchored keys pass.
```

```text
The check catches an alternative that would reach past the Reports module.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs\:\:TheModulesReportsAlternativeCheck\_NamesAMadeUpAlternativeThatReachesALookalike\_AndPassesTheAnchoredOnes

```text
An alternative anchored at the start but open at the end on a full-name subject
```

###### Obligations



##### Covers

P3

```text
The test project's source files are read as text for string literals that name Reports by its old Services path: plain dotted, as a folder path, as a regular expression, below Reports, or as one choice of a group under Services; literals that are a governed rule pattern or a piece of one are left to the role check.
```

```text
The old-path literals are exactly the entries of the fixed rename list: none outside it, and every entry still found in its file.
```

```text
No test keeps pointing at the old Reports place by a string the move would leave behind.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs\:\:NoRuleStringNamesReportsByItsOldPath\_OutsideTheRenameList

```text
A string assembled at run time from fragments none of which names the old path alone
```

###### Obligations



##### Covers

P3

```text
The same comparison is run over a made-up rule file and a made-up rename list with one entry still in the file and one gone.
```

```text
It names the four unlisted old-path forms (old namespace, old port-file path, old Metrics namespace, a group naming Reports) and the entry that is gone, and not the listed string, a lookalike namespace, a test namespace, a governed pattern or a piece of one.
```

```text
The rename list is checked both ways, so it can only shrink as the move proceeds.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsRulesSeeTheMoveArchUnitTest\.cs\:\:TheRenameListCheck\_NamesEachUnlistedOldPathString\_AndAListedStringThatNoLongerExists

```text
A literal that is a piece of a governed pattern but names a second old path the pattern does not
```

###### Obligations



##### Covers

P4

```text
The shared preservation law, now also used with the Reports map, is asked again with the platform map over the whole test assembly, and every architecture test runs beside the new Reports check.
```

```text
Every platform verdict is unchanged and every existing architecture test stays green; no production file changes.
```

```text
Nothing a user, a tracker or the database sees changes, and the rules that already held still hold.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:EveryRulePatternThatSeesAMovingPlatformType\_IsDeclared\_AndKeepsItsRoleOnTheSubjectItsRuleReads

```text
A behaviour change in production code that no architecture rule observes; the backend suite run by the host covers it
```

###### Obligations


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
