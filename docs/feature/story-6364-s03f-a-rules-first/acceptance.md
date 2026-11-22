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
