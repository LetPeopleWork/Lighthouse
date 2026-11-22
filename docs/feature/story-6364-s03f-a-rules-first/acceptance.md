# Acceptance brief: story\-6364\-s03f\-a\-rules\-first

## Slices

### S1

##### Covers

P1

```text
The architecture rule tests read every top-level type in the namespaces the platform lane lives in today and under the new platform namespace, and compare them with the map of types that move, each with its area and old namespace, and types that stay, each with its owner.
```

```text
Today 165 types are listed to move across 11 areas and 63 types are listed to stay with five owners; each listed type is found once in its old or its new namespace and no other type is found. A type left off is named as not on the map, a made-up or vanished name as not in the code, a type in both namespaces as found twice, a type in the wrong area or a namesake in another lane namespace by its full name; a type found in its new namespace still passes; nested types fold into their outer type and a look-alike namespace is not read.
```

```text
The move has a fixed, measured list of what moves where and what stays with which owner, and any drift from it fails naming the type.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:\:TheTypesOfThePlatformLaneInTheCompiledCode\_AreExactlyTheTypesTheMapListsToMoveOrToStay

```text
A type added later in a Services namespace the lane does not use today (for example Reports or Refinement) is not read; the lane's namespaces are fixed by the pattern.
```

###### Obligations









##### Covers

P1

```text
The rule tests find every class in the compiled code that takes the domain-event handler contract, directly or through a base class, under its old or its new name.
```

```text
Some handlers are found inside the modules and none outside them. A made-up handler in a Services namespace, one through its base class and one in a namespace that only starts like Modules are named; one in a module is not.
```

```text
Domain-event handlers stay with the module that reacts; only the dispatcher and its contracts are platform.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:\:NoDomainEventHandlerInTheCompiledCode\_LivesOutsideTheModules

```text
A handler wired by a delegate that never implements the contract type is not seen; the check reads only types that implement the contract.
```

###### Obligations




##### Covers

P1

```text
The rule tests read the suppression file and the analyser-config files of both projects and look for targets that name a moving type by its old or its new full name.
```

```text
Exactly three targets are found and listed, all in the production suppressions file: RandomNumberService.GetRandomNumber with the domain-events-and-host move, PostgresDatabaseManagementProvider with the database-and-seeding move, LighthouseReleaseService with the usage-data-logging-and-releases move. An unlisted target naming a moving type is named, one naming a type that stays is not, a listed target rewritten to the new namespace still matches, and one no longer in its file is named.
```

```text
The move rewrites every suppression target that names a moved type in the same commit, so no analyser warning comes back.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:\:TheSuppressionAndAnalyserTargetsNamingAMovingType\_AreExactlyTheOnesListed

```text
A target naming a moving type by its short name only, without a namespace, is not found; analyser targets here are written by full name.
```

###### Obligations




##### Covers

P1

```text
The rule tests judge the eleven shared Models types of the key ring and of logging against the four conditions for joining the lane, and five made-up candidates in the test code.
```

```text
None of the eleven is admitted, so none is listed to move; each refusal names its condition (sent by a controller, reached by a Models type that stays, used outside the lane). The made-up candidates come out stored, on the wire, reached by the kernel, used outside the lane, and admitted when only the lane uses it.
```

```text
No shared Models type leaves the kernel unless nothing left behind needs it.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:\:TheModelsTypesListedToMove\_AreExactlyTheCandidatesThatMeetAllFourConditions

```text
A Models type sent through a variable typed object, or built by a helper the controller does not call directly, is not seen as on the wire.
```

###### Obligations




##### Covers

P1

```text
The rule tests look up each string the move must rewrite, a full type name or a source path in a test or script file, in its file under its old or its new text.
```

```text
All 43 listed strings, in 18 files, are found; a made-up listed string its file does not hold is named and a real one is not.
```

```text
Each move knows which test and script strings name its types, so none is left pointing at a type that moved.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneMembershipArchUnitTest\.cs\:\:EachStringTheMoveRenames\_IsFoundInItsFile\_UnderItsOldOrItsNewText

```text
A string naming a moving type that is not on the list is not found here; completeness over the rule files is the old-path literal check of the rules slice.
```

###### Obligations



##### Covers

P6

```text
The backend build and the whole backend suite run after the oracle and its map are added.
```

```text
No production file changes; every existing test stays green.
```

```text
Nothing a user, a tracker or the database sees changes.
```

##### Disposition

NotTested

```text
This slice adds test files only; behaviour preservation is observed by the existing backend suite, the build and EXAMINE, not by an architecture rule over the compiled code.
```

crafter and reviewer\, through the backend gate

### S2

##### Covers

P2

```text
The architecture tests ask every constant rule pattern of the test assembly, found by reflection, about each moving platform type under its old Services name and its new Platform name.
```

```text
Every pattern gives the same answer under both names; a pattern that differs is named with the moving type and both answers. Today 31 patterns differ.
```

```text
No architecture rule silently stops seeing a platform type when the type moves into Platform.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:EveryRulePatternOfTheTestAssembly\_AnswersTheSameForEachMovingPlatformType\_UnderItsOldAndItsNewNamespace

```text
A bare word or test-data constant that does not name Lighthouse.Backend and is not anchored loses sight of a moved type; such constants are not asked, by the documented rule-pattern criterion.
```

###### Obligations



##### Covers

P2

```text
The same preservation check runs over made-up rule keys, one of them in a nested class, that see only the old place, both places, neither place, or are not a pattern.
```

```text
Only the key in the nested class that sees only the old place is named; the keys that see both or neither, and the string that is not a pattern, pass.
```

```text
A rule added later in any test class, nested or not, is checked without anyone listing it.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:ThePreservationCheck\_NamesAMadeUpPatternInANestedClassThatSeesOnlyTheOldPlace\_AndPassesOnesThatSeeBothOrNeither

```text
A check that skipped classes outside the made-up keys namespace would pass this row; the real-pattern row and the update-queue rule row catch that.
```

###### Obligations





##### Covers

P2

```text
The preservation check meets a constant full name of one moving type that the membership map lists as renamed by a move, and a namespace rule of the update queue.
```

```text
The full name is left out although its two answers differ; the update-queue rule is taken up.
```

```text
A full name of one type is renamed by its move, not widened to name two types.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:ThePreservationCheck\_LeavesAFullNameTheMoveRenames\_ToTheMoveThatRenamesIt

```text
A renamed value matched in another class of the same listed file; the skip matches by the class name of the file, so only a same-named class there would slip.
```

###### Obligations



##### Covers

P2

```text
Every rule pattern that mentions Platform is asked about every production type outside Lighthouse.Backend.Platform, with and without its Platform alternatives.
```

```text
No production type outside Platform is matched only through a Platform alternative.
```

```text
Widening a rule for the move puts no unrelated type under it.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:NoPlatformAlternativeOfARulePattern\_MatchesAProductionTypeOutsidePlatform

```text
A widening to all of Lighthouse.Backend written without the word Platform; the existing architecture tests over compliant code and the preservation law answer that case.
```

###### Obligations



##### Covers

P2

```text
The Platform-alternative check runs over a made-up key with an unanchored Platform alternative and one with an anchored Platform.DomainEvents alternative.
```

```text
The unanchored key is named for production types such as the platform service interface; the anchored key passes.
```

```text
An over-wide alternative is caught before it hides as a passing rule.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:ThePlatformAlternativeCheck\_NamesAMadeUpAlternativeThatReachesOutsidePlatform\_AndPassesAnAnchoredOne

```text
Platform written right after a character other than an escaped dot, an opening bracket or a bar, which the token match does not take out.
```

###### Obligations


##### Covers

P2

```text
The dispatcher selection of the handler rules runs over the product and over the test assembly, which holds types whose namespace only spells the old dispatcher namespace.
```

```text
In the product it selects the dispatcher and its nested types, under the old or the new name; in the test assembly it selects nothing. Today it selects the test types that spell the old namespace.
```

```text
The handler rules keep checking the event dispatcher after it moves.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:TheDispatcherSelection\_SelectsTheDispatcherAndItsNestedTypesByType\_AndNoTypeThatOnlySpellsItsOldNamespace

```text
A selection by the simple name of the dispatcher instead of its type, which picks only the dispatcher in the product and nothing in the test assembly today.
```

###### Obligations



##### Covers

P3

```text
Every string in the architecture rule files is read as text and looked at for a moving area as a whole, or a moving type, under its old Services path.
```

```text
No such string is left except what the membership map renames for that file and the constant rule patterns the preservation check governs. Today four are named: the dispatcher fragment and its two descriptions in the core rules, and the update-contracts hint in the Forecasting rules.
```

```text
No rule string keeps pointing at the old place after the move.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:NoStringInTheRuleFiles\_NamesAMovingAreaOrMovingTypeByItsOldPath\_UnlessTheMoveRenamesIt

```text
An old path built by concatenating separate fragments, or written in a raw string literal, which the text scan does not join or read.
```

###### Obligations



##### Covers

P3

```text
The old-path check reads made-up strings: the inline dispatcher fragment, an old-area message, a moving file path, a non-constant pattern, the full name of a moving type, a staying handler test, a staying update pipeline, a test class name and a governed rule pattern; and the full name again in the file the map renames it in.
```

```text
The first five are named with file and line; the other four pass; the full name passes in the file the map renames it in.
```

```text
The check names old paths of moving things only, so it is not switched off for noise.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:TheOldPathCheck\_NamesMadeUpStringsThatNameTheOldPlace\_AndPassesTheOnesThatAreNotAboutTheMove

```text
A moving type named by its short name alone, without its old namespace, which is not an old path and is not looked for.
```

###### Obligations



##### Covers

P6

```text
The whole backend suite, live connectors left out, runs on the code as it is today after the rules are widened.
```

```text
Every existing test stays green; no production file changes.
```

```text
Nothing a user, a tracker or the database sees changes.
```

##### Disposition

Tested

Lighthouse\.Backend full suite under the CLAUDE\.md filter that leaves out the live\-connector categories

```text
A widening that changes which real types a rule examines without changing any verdict on today code; the preservation and Platform-alternative rows bound that.
```

###### Obligations


### S3

##### Covers

P4

```text
A change touches only the GitHub service, the Lighthouse release service, one of their interfaces or one of their tests, now kept with the platform's release code
```

```text
CI test selection runs the live GitHub suite and the backend suite for that change, and no tracker live suite
```

```text
A change to the release code cannot reach main without the suite that talks to GitHub having run
```

##### Disposition

Tested

Scripts\/test\-selection\/test\-path\-classifier\-platform\.sh

```text
A widening that also selects the GitHub suite for other release-area files not checked here, such as a nested folder under the release area; the other-file and other-area cases catch the broad widenings
```

###### Obligations




##### Covers

P4

```text
A change touches only platform code that does not talk to GitHub: another file of the release area, or a file of another platform area such as encryption
```

```text
CI test selection runs the backend suite for that change and no live suite, the GitHub one included
```

```text
Platform changes that cannot break GitHub do not spend live GitHub quota, and still run the backend tests
```

##### Disposition

Tested

Scripts\/test\-selection\/test\-path\-classifier\-platform\.sh

```text
A widening that selects the GitHub suite for a platform area not among the two checked; the encryption case stands for every other area and the assembly-service case for the rest of the release area
```

###### Obligations


##### Covers

P4

P6

```text
Every change the classifier already handled is classified again, among them the GitHub and release service at their old service paths
```

```text
Every one keeps its earlier selection: the old release-code paths still run the live GitHub suite, and no other suite selection changes
```

```text
CI runs the same suites as before for every change it already knew; nothing else a contributor sees changes
```

##### Disposition

Tested

Scripts\/test\-selection\/test\-path\-classifier\.sh

```text
A change that alters the selection for a path neither script lists; the existing cases cover each pattern of the classifier
```

###### Obligations



## Expectations

### Rows

#### E1

##### Promises

P1

```text
A contributor can run the architecture-rules check and see that the platform lane's membership (each platform type with its area, old and new namespace, and the files that stay with their owner) is pinned: the check passes while every listed type sits in its old or new namespace, and names the offender when a listed type is missing or a listed name does not exist. It also reports no domain-event handler outside Modules, and lists every suppression or analyser-config target naming a moving type.
```

#### E2

##### Promises

P2

```text
A contributor sees by running architecture-rules and backend-tests that each existing architecture rule pattern answers the same for a moving platform type under its new Platform namespace as under its old one, that no added alternative matches a production type outside Platform, and that the old inline DomainEvents fragment is gone, replaced by the exact dispatcher type.
```

#### E3

##### Promises

P3

```text
A contributor reading the rule files, and the architecture-rules check, finds no string naming a moving area (Encryption, DatabaseManagement, UsageData, Logging, DomainEvents, BackgroundServices.Update, Interfaces.Update) or moving loose type by its old Services.Implementation, Services.Interfaces or Services/Implementation path, other than entries the membership list marks as renamed by a later move slice.
```

#### E4

##### Promises

P4

```text
Asking test-selection-show about a changed Platform/Releases/GitHubService.cs, or its test path, shows the GitHub live suite selected; paths that were selected before (or deliberately not selected) still show the same selection as before.
```

#### E5

##### Promises

P5

```text
The platform-answers-compare examine command is declared and admitted before any file moves; platform-answers-selftest shows it works: it prints DIFFERENT and exits 1 when one platform answer (system info, version without GitHub calls, encryption/secrets state, database-management status, usage-data state, log level and problems, update status) differs between two revisions, prints IDENTICAL and exits 0 when none differ, ignores volatile fields, and fails (not IDENTICAL) when nothing was compared.
```

#### E6

##### Promises

P6

```text
Running backend-tests shows every existing test green, and the platform answers compared across the pre-feature and feature revisions come out IDENTICAL: nothing a user, tracker or the database sees changes, and no production file moved or changed behaviour in feature A.
```

## Charter

None.

## Examination

None.
