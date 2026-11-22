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
Every regex rule field of the test assembly, constant or static read-only, is asked on the subject its rule reads (namespace or full name) about every moving platform type under its old and its new place
```

```text
Every field that sees a moving type is declared with a role; a part of the code also sees the type in its new namespace, only through the whole area; an old location does not see the new place and carries no Platform alternative; each failure names the field, subject, role, type and both answers
```

```text
No architecture rule silently stops seeing a platform type, or starts judging the new place as an old location, when the type moves
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:EveryRulePatternThatSeesAMovingPlatformType\_IsDeclared\_AndKeepsItsRoleOnTheSubjectItsRuleReads

```text
a role declared wrongly in PlatformLanePatternRoles (a forbidden target declared as an old location); review of the declared reasons guards that
```

###### Obligations











##### Covers

P2

```text
The same role check runs over made-up rule keys in a nested fixture class: a namespace rule seeing only the old place, one widened by a type name, the same text read on full names, an old location widened into or given a type name in the new place, a whole-area widening, a widening into an unseen area, undeclared keys, composites of a blind and of a sound key, and a designation read from a type
```

```text
Exactly the blind, type-name, widened-old-location, unseen-area and undeclared keys and the blind composite are named, each with its kind; the full-name twin, the whole-area widening, the sound composite and the designation pass
```

```text
The check is shown to tell a rule that keeps its meaning from one that breaks it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:TheRoleCheck\_NamesEachMadeUpPatternThatBreaksItsRole\_AndPassesTheOnesThatKeepIt

```text
a check that reads only fixture keys and misses real ones; the real test's count guard of more than fifty fields covers that
```

###### Obligations










##### Covers

P2

```text
The role check enumerates the rule fields, including a full name the membership map lists as renamed by the key-ring move
```

```text
The renamed full name is left out and a real update-queue rule is taken up
```

```text
A designation of one type is left to the move that renames it rather than widened to name two types
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:ThePreservationCheck\_LeavesAFullNameTheMoveRenames\_ToTheMoveThatRenamesIt

```text
a check that skips every full-name-shaped constant, not only the renamed ones
```

###### Obligations


##### Covers

P2

```text
Every Platform alternative of a rule field is asked, on the field's declared subject, about the production namespaces and full names outside Lighthouse.Backend.Platform
```

```text
No field matches anything outside Platform only through its Platform alternative
```

```text
A widening for the move puts no unrelated code under a rule
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:NoPlatformAlternativeOfARulePattern\_MatchesAProductionNamespaceOrTypeOutsidePlatform

```text
an alternative that would reach outside only through a namespace no production type uses yet
```

###### Obligations



##### Covers

P2

```text
The Platform-alternative check runs over the made-up keys, one of them carrying an unanchored Platform alternative read on full names
```

```text
Only that key is named, matching the platform service outside Platform; the anchored keys pass
```

```text
The check is shown to catch a too-wide alternative
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:ThePlatformAlternativeCheck\_NamesAMadeUpAlternativeThatReachesOutsidePlatform\_AndPassesAnAnchoredOne

```text
a check that asks a namespace-declared key only about full names
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



### S4

##### Covers

P5

```text
Two platform answer captures that differ only in the log level are compared
```

```text
The verdict is DIFFERENT with exit 1; the log level is reported as 1 compared and 1 different, the unchanged system info as 1 compared and 0 different, and the differences name the log level answer
```

```text
The examiner sees which platform answer a move changed
```

##### Disposition

Tested

Scripts\/examine\/test\-platform\-answers\-compare\.sh

```text
A compare that reports the right counts and names the file but prints the differences of every answer, not only the changed one, would still pass
```

###### Obligations


##### Covers

P5

```text
Two identical platform answer captures held in separate folders are compared
```

```text
The verdict is IDENTICAL over all 12 captured answers with exit 0
```

```text
The examiner can trust a matching answer comparison after the move
```

##### Disposition

Tested

Scripts\/examine\/test\-platform\-answers\-compare\.sh

```text
A compare that says IDENTICAL whenever both folders hold the same answer names would pass here; the changed-answer case catches it
```

###### Obligations


##### Covers

P5

```text
Two captures from different runs differ only in the process id, the install time and when a problem was recorded
```

```text
The verdict is IDENTICAL over all 12 captured answers with exit 0
```

```text
Two runs of an unchanged revision match, so values that change on every run do not mislead the examiner
```

##### Disposition

Tested

Scripts\/examine\/test\-platform\-answers\-compare\.sh

```text
A compare that masks more keys than the values of the run would pass here; the stable-beside-volatile case catches it
```

###### Obligations


##### Covers

P5

```text
Two captures from different runs differ in the database provider and in a recorded problem's message, beside the values that change on every run
```

```text
The verdict is DIFFERENT with exit 1, and both the system info and the recorded problems are reported as 1 compared and 1 different
```

```text
A real change sitting next to a run value is still caught
```

##### Disposition

Tested

Scripts\/examine\/test\-platform\-answers\-compare\.sh

```text
A mask that blanks a stable field other than the database provider or the problem message, such as the operating system, would still pass
```

###### Obligations



##### Covers

P5

```text
Two empty capture folders are compared
```

```text
The command does not exit 0, never says IDENTICAL, and says nothing was compared
```

```text
An empty capture is never taken as proof that nothing changed
```

##### Disposition

Tested

Scripts\/examine\/test\-platform\-answers\-compare\.sh

```text
A command that always fails saying nothing was compared would pass here; the identical case catches it
```

###### Obligations


##### Covers

P5

```text
The examiner reads the usage of the platform answers command, the list of admitted examine probes, and runs the selftest
```

```text
The usage names the system info, the version with the GitHub calls left out, the encryption and secrets state, the database-management status, the usage-data state, the log level and problems, and the update status; the probe list admits the compare command and its selftest; the selftest runs these checks and passes with their total
```

```text
The examiner can run the probe before any platform file moves and knows what a matching run proves
```

##### Disposition

Tested

Scripts\/examine\/test\-platform\-answers\-compare\.sh

```text
A usage that names an answer the capture does not actually read would still pass; only the full capture run by the examiner shows that
```

###### Obligations


##### Covers

P6

```text
The existing Access and Licensing answers checks run after the new command is added
```

```text
They still pass, so the shared capture and compare pieces keep their behaviour
```

```text
Existing examine commands answer as before
```

##### Disposition

Tested

Scripts\/examine\/test\-access\-answers\-compare\.sh

```text
A change to the shared pieces that only alters a command other than access-answers-compare would still pass this check
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
