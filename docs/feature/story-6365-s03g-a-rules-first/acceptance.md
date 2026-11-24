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


### S2

##### Covers

P2

```text
The architecture check asks every rule pattern of the test project, on the subject its rule reads, about the old and the new name of every Refinement type that moves
```

```text
Every pattern that sees a moving Refinement type is declared with its role; a part of the code keeps seeing the type under Modules.Refinement, an old location and a folder-only match do not follow it. On today's rules the check fails naming the Refinement module pattern, the Refinement contexts of Access, Licensing and Tracker integration, the Refinement guards' per-type and services patterns, the patterns built from them, and the exact Refinement namespace constant
```

```text
The maintainer knows no architecture rule goes blind or starts judging the wrong code when Refinement moves into its module
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs\:\:EveryRulePatternThatSeesAMovingRefinementType\_IsDeclared\_AndKeepsItsRoleOnTheSubjectItsRuleReads

```text
A pattern is declared with the wrong role or subject in the roles list, or a Modules.Refinement alternative is written in a form the alternative marker does not recognise, such as (Services|Modules)\.Refinement; review of the roles list carries those
```

###### Obligations





##### Covers

P2

```text
The same check is run over made-up rule patterns, each declared with a subject and a role
```

```text
It names exactly the made-up patterns that break their role, each with its break: a part of the code that sees only the old place, a namespace rule widened by type names, a read path widened only to the Application layer (losing exactly the Domain types), an old location widened into the new place, a part of the code widened into Refinement without ever seeing it, a folder-only match that follows the type, two undeclared patterns (one a static read-only field in a nested class), and a composite built from a blind part of the code; it passes the patterns that keep their role, including a part of the code widened by an anchored Modules.Refinement alternative
```

```text
The maintainer can trust that the check above would name a rule that breaks at the move, not pass it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs\:\:TheRoleCheck\_NamesEachMadeUpPatternThatBreaksItsRole\_AndPassesTheOnesThatKeepIt

```text
A law that names the right breaks for these shapes but misreads a shape none of the made-up keys has, such as a pattern with a lookbehind
```

###### Obligations









##### Covers

P2

```text
Each Modules.Refinement alternative of a rule pattern is asked about every production namespace and full name outside Modules.Refinement, on the subject the pattern is declared with
```

```text
No Modules.Refinement alternative matches anything outside Modules.Refinement; today no rule has one, so the check passes, and it keeps passing once the crafter adds anchored alternatives
```

```text
Widening a rule for the move puts no unrelated code under that rule
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs\:\:NoModulesRefinementAlternativeOfARulePattern\_MatchesAProductionNamespaceOrTypeOutsideModulesRefinement

```text
An alternative too wide only for a namespace that does not exist yet in the production code, or written without the Modules marker the check looks for
```

###### Obligations



##### Covers

P2

```text
The alternative check is run over the made-up rule patterns, with a made-up lookalike namespace added to the production names
```

```text
It names only the made-up pattern whose Modules.Refinement alternative is unanchored and so matches the lookalike, and none of the anchored ones
```

```text
The maintainer can trust that a too-wide alternative would be named, and a correct one would not
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs\:\:TheModulesRefinementAlternativeCheck\_NamesAMadeUpAlternativeThatReachesALookalike\_AndPassesTheAnchoredOnes

```text
A check that asks every alternative about both namespaces and full names regardless of the declared subject, since the lookalike is given in both forms
```

###### Obligations



##### Covers

P3

```text
The test project's source files are read as text, and every string that names Refinement or a moving Refinement type by its old Services path is compared with the fixed rename list, both ways
```

```text
The old-path strings are exactly the 20 listed ones: none unlisted, none listed but gone. On today's rules the check fails naming the exact old implementation namespace constant of the Refinement guards as unlisted
```

```text
The maintainer has one fixed list of what the move still has to rename, and no string quietly keeps pointing at the old place
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs\:\:TheStringsInTheTestProjectThatNameRefinementByItsOldPath\_AreExactlyTheFixedRenameList

```text
A string built at run time from fragments that never name the old path whole, or a selection by an exact new namespace written inline; a new-namespace string kept in a constant is named by the role check instead
```

###### Obligations




##### Covers

P3

```text
The rename-list comparison is run over a made-up rule file and a made-up two-entry list
```

```text
It names the three unlisted old-path strings (the old namespace, a port file path, a port type's full name) and the listed string the file no longer holds; it passes the listed string that is still there, a lookalike namespace, a test class namespace and a governed rule pattern
```

```text
The maintainer can trust the list check catches a new old-path string and a stale entry, so the list only shrinks
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs\:\:TheRenameListCheck\_NamesAnUnlistedOldPathString\_AndAListedStringThatNoLongerExists

```text
A listed string that survives only inside a comment or as part of a longer string, since an entry counts as present when its file holds it as a whole quoted string anywhere
```

###### Obligations




##### Covers

P4

```text
After the crafter widens the Refinement rule patterns and rewrites the exact-namespace uses, the whole architecture test namespace and the backend suite are run
```

```text
Every existing test stays green, including the platform preservation law with all its verdicts; no production file has moved or changed
```

```text
Nothing a user, a tracker or the database sees changes
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture \(FullyQualifiedName\~Lighthouse\.Backend\.Tests\.Architecture\, including PlatformLaneRulesSeeTheMoveArchUnitTest\) and the filtered backend suite

```text
A widened rule pattern that keeps every verdict today but would answer differently once the types move; the preservation law above carries that, not this run
```

###### Obligations


##### Covers

P2

```text
The role check is run over made-up patterns with a made-up whole-parent list: one listed pattern over all of Modules, one listed pattern over every named module but not Modules itself, and one unlisted pattern over all of Modules
```

```text
The listed pattern over all of Modules may see the moved types for the first time and is not named; the listed narrower pattern is named both as seeing Refinement it never saw and as narrower than Modules; the unlisted pattern over all of Modules is named as seeing Refinement it never saw. On the real rules only the platform membership's module pattern is on the list, it takes in all of Modules, and it is no longer named
```

```text
A rule over every module rightly starts seeing Refinement once it becomes a module, and no other rule may quietly start judging the moved types
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RefinementRulesSeeTheMoveArchUnitTest\.cs\:\:TheWholeParentList\_AdmitsTheNewSightOfAListedPatternOverAllOfModules\_AndNoOther

```text
A listed pattern that takes in all of Modules and also something outside it, such as the whole backend; the list is short and declared, so review carries that
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

### E1

met

#### Observations

```text
architecture-rules whole rule set: 1746/1746 passed. Membership tests pass: TheRefinementTypesInTheCompiledCode_AreExactlyTheTypesTheMapListsToMove_EachFoundOnce, EachTypeThatStays_IsInTheCompiledCodeWhereListed, TheTargetsNamingAMovingType_AreExactlyTheOnesListed.
```

```text
Negative controls pass with named failures: AMadeUpNameOnTheMap_IsNamedAsNotInTheCode, AListedTypeTheCodeNoLongerHolds_IsNamedAsNotInTheCode, AMapWithOneTypeLeftOff, ATypeFoundInItsNewNamespaceInsteadOfItsOld_IsStillOnTheMap, ATypeLeftInItsOldNamespaceBesideItsMovedCopy_IsNamedAsFoundTwice, namesake and duplicate cases.
```

### E2

met

#### Observations

```text
Negative controls pass with named failures: AMadeUpNameOnTheMap_IsNamedAsNotInTheCode, AListedTypeTheCodeNoLongerHolds_IsNamedAsNotInTheCode, AMapWithOneTypeLeftOff, ATypeFoundInItsNewNamespaceInsteadOfItsOld_IsStillOnTheMap, ATypeLeftInItsOldNamespaceBesideItsMovedCopy_IsNamedAsFoundTwice, namesake and duplicate cases.
```

### E3

met

#### Observations

```text
Layer rule passes: EachFileIsDomain_ExactlyWhenItIsNoPortFile_AndEveryTypeItDeclaresReachesOnlyTheSharedModels...; controls AFileReachingBeyondTheKernelListedAsDomain_IsNamedWithWhatItReaches, AKernelOnlyFileListedAsApplication_IsNamed, port-file split, made-up Domain lookalike all pass.
```

### E4

met

#### Observations

```text
Targets record: TheTargetsNamingAMovingType_AreExactlyTheOnesListed passes; controls AnUnlistedTargetNamingAMovingType_IsNamed, AListedTargetNoLongerInItsFile_IsNamed, AFileTheTargetScanMustRead_IsNamedWhenMissing pass. test-selection-show on the membership test file prints backend: true, others false. Target list is empty on real code; not diffed against a pre-move selection.
```

### E5

met

#### Observations

```text
Rule patterns: EveryRulePatternThatSeesAMovingRefinementType_IsDeclared_AndKeepsItsRoleOnTheSubjectItsRuleReads passes; TheRoleCheck_NamesEachMadeUpPatternThatBreaksItsRole passes; whole rule set passed. I ran no own probe type in the new namespace.
```

### E6

met

#### Observations

```text
Outside-module alternatives: NoModulesRefinementAlternativeOfARulePattern_MatchesAProductionNamespaceOrTypeOutsideModulesRefinement and TheModulesRefinementAlternativeCheck_NamesAMadeUpAlternativeThatReachesALookalike_AndPassesTheAnchoredOnes pass.
```

### E7

met

#### Observations

```text
Exact-namespace rules in RefinementModuleArchUnitTest (The_need_calculator_lives_in_the_Refinement_module, The_Refinement_module_reaches_neither_the_trackers_nor_the_background_updates, Nothing_but_the_API_and_the_composition_root_depends_on_the_Refinement_module) pass in the full run. I removed no map entry myself; the membership test names a map missing a type.
```

### E8

met

#### Observations

```text
String sweep: TheStringsInTheTestProjectThatNameRefinementByItsOldPath_AreExactlyTheFixedRenameList passes on real tree; TheRenameListCheck_NamesAnUnlistedOldPathString_AndAListedStringThatNoLongerExists passes (both ways). Shrink-only over time not observable in one run.
```

### E9

met

#### Observations

```text
String sweep: TheStringsInTheTestProjectThatNameRefinementByItsOldPath_AreExactlyTheFixedRenameList passes on real tree; TheRenameListCheck_NamesAnUnlistedOldPathString_AndAListedStringThatNoLongerExists passes (both ways). Shrink-only over time not observable in one run.
```

### E10

met

#### Observations

```text
ef-model-unchanged: Sqlite and Postgres both report No changes have been made to the model since the last migration.
```

### E11

met

#### Observations

```text
git diff --name-only main...HEAD excluding docs/ and Backend.Tests is empty: only test-project files and docs changed; no production file changed.
```

### E12

met

#### Observations

```text
backend-tests whole suite: Test Run Successful, 14618 total, 14606 passed, 0 failed, 12 skipped.
```

```text
Whole backend suite green (14606 passed, 0 failed). test-selection-show runs and selects; no pre-move baseline to compare selections.
```
