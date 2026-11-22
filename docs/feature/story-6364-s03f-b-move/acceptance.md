# Acceptance brief: story\-6364\-s03f\-b\-move

## Slices

### S1

##### Covers

P1

```text
Read the compiled backend for every type the platform membership map gives the key-ring move, in its new platform area and in its old namespace
```

```text
Each type is found exactly once in the Encryption platform area, none is left in its old namespace, and no listed name matches nothing
```

```text
A developer finds the key ring, secret custody, the crypto service and the key-ring file watcher under the platform Encryption area only
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformEncryptionLayoutArchUnitTest\.EveryKeyRingType\_LivesInItsPlatformArea\_AndNoneIsLeftInItsOldNamespace

```text
a moved type that kept an extra copy under a namespace the map never lists as old; member or behaviour changes, which the existing suite and EXAMINE probes guard
```

###### Obligations






##### Covers

P6

```text
Read the compiled test assembly for each unit-test class of the moved key-ring types, in the platform Encryption test namespace and in its old namespace, and for the two tests of shared Models types
```

```text
Each moved test class is found once in the platform Encryption test namespace and none in its old one; the two tests of Models types stay where they were
```

```text
A developer finds the key-ring tests next to their subjects under the platform test area, and the Models tests unmoved
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformEncryptionLayoutArchUnitTest\.TheUnitTestsOfTheMovedTypes\_LiveInTestsPlatformEncryption\_AndNoneIsLeftInItsOldNamespace and TheTestsOfTheModelsTypesThatStay\_StayInTheirOldNamespace

```text
a test class moved but emptied of its tests; the existing suite guards that
```

###### Obligations




##### Covers

P1

```text
Read the test-time budget and its Python check for the strings the map lists as renamed by the key-ring move
```

```text
Both files name the four budgeted Encryption test classes by their new full names and no longer by their old ones
```

```text
The test-time budget still applies to the secret custody, published key count, referenced key ids and readable-secrets tests after they move
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformEncryptionLayoutArchUnitTest\.TheTimeBudgetAndItsCheck\_NameTheBudgetedEncryptionTestClassesInTheirNewPlace

```text
a budget value changed together with the key; the budget check itself runs in the declared pytest command
```

###### Obligations

None.

##### Covers

P6

```text
Run the subject-aware preservation check over every rule pattern of the test assembly, with the crypto service full-name constant holding either its old or its new text
```

```text
The check leaves a constant holding either text of its renamed row to the move, keeps taking up the update-queue rule, and stays green before and after the move
```

```text
Architecture rules keep guarding the key-ring types in their new place; none goes blind at the move
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneRulesSeeTheMoveArchUnitTest

```text
a constant holding the text of a renamed row of its own file is skipped even if it is really a namespace rule; the map row is then the owner
```

###### Obligations

None.

##### Covers

P7

```text
Run the full filtered backend suite and the platform comparison probes after the move
```

```text
Every existing test stays green and every platform answer, reaction order and database model is the same as before
```

```text
Nothing a user, an API client, a tracker or the database sees changes
```

##### Disposition

Tested

Full filtered backend suite \(live connectors excluded\) and the EXAMINE probes platform\-answers\-compare\, refresh\-reactions\-compare and ef\-model\-unchanged

```text
a behaviour change that no existing test or probe observes
```

###### Obligations

None.

##### Covers

P6

```text
Run the membership map check with a made-up copy of the crypto service beside it, and with the crypto service moved into the wrong platform area, taking the example from the map row and from whichever full name the compiled code holds
```

```text
The check names the crypto service as found twice in the first case, and names it as not in the code and the wrong-area name as not on the map in the second, both before and after the key-ring move
```

```text
The membership map keeps catching a type left behind or moved to the wrong place after the key ring moves; it does not go red because of the move itself
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneMembershipArchUnitTest

```text
the example type is moved by a later slice in a way the map row does not list; the map row is then the owner
```

###### Obligations

None.

##### Covers

P6

```text
The preservation law reads every rule pattern against the B1 moves, with KeyRingFileWatcher listed in PlatformLanePatternRoles.FolderOnlyMatches as seen only by its BackgroundServices folder by TrackerIntegrationModuleBoundariesArchUnitTest.PromisesAndTheUpdateCode
```

```text
PromisesAndTheUpdateCode and its composite OtherContexts keep no Platform.Encryption alternative: following the watcher into Platform.Encryption is named FollowsATypeItSawOnlyByFolder, any other Encryption type it then sees is named SeesAnAreaItNeverSaw; every other role and widening is unchanged; a made-up folder-only key widened into the new place is named and one left in the old place passes
```

```text
Tracker integration may still sit on the key ring after it moves to Platform/Encryption, and its other-contexts rule takes in none of Platform.Encryption, which it never covered
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneRulesSeeTheMoveArchUnitTest\.cs\:\:EveryRulePatternThatSeesAMovingPlatformType\_IsDeclared\_AndKeepsItsRoleOnTheSubjectItsRuleReads

```text
the Encryption area is widened into PromisesAndTheUpdateCode or a composite built from it, or the folder-only exception is dropped from the law (the made-up falsifier then fails)
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Key ring, secret custody, CryptoService, key-ring file watcher and admitted Encryption types are all under Platform/Encryption with namespace equal to folder and none in an old namespace; the architecture-rules layout oracle passes and the time-budget check (test-time-budget.json and its Python test) still finds the four Encryption test classes by their new names (test-selection-show, backend-tests).
```

#### E2

##### Promises

P2

```text
Database-management, demo-data, seeding (ISeeder, InstallTimestampSeeder) and persistence (LostRaceTolerantSave) types sit in their Platform folders with namespace equal to folder; the seven context seeders stay in place and still seed; dotnet build stays clean with the suppression naming the new full name; the architecture-rules layout oracle and backend-tests pass.
```

#### E3

##### Promises

P3

```text
Usage-data, logging and release types sit under Platform/UsageData, Platform/Logging and Platform/Releases with namespace equal to folder; the usage-data pipe sends the same events (platform-answers-compare usage-data state identical); a change under Platform/Releases still selects the GitHub live suite (test-selection-show) and that suite passes (ci-live-suites-show).
```

#### E4

##### Promises

P4

```text
Domain-event dispatcher, its interfaces, clock, RandomNumberService and host facilities sit under Platform/DomainEvents or Platform/Host with namespace equal to folder; handlers stay with their contexts; domain-event reactions occur in the same order (refresh-reactions-compare IDENTICAL); the moved tests are named at their new place in the architecture lists and the build is clean.
```

#### E5

##### Promises

P5

```text
Update-queue mechanics types sit under Platform/UpdateQueue with namespace equal to folder and none in an old namespace; TeamUpdater, PortfolioUpdater, UpdateServiceBase<T> and their three interfaces are unmoved; update status is identical before and after (platform-answers-compare); architecture-rules layout oracle passes.
```

#### E6

##### Promises

P6

```text
Unit tests of moved types sit under Tests/Platform/<Area> with namespace equal to folder; every architecture rule and list naming a moved type uses its new place, and the subject-aware preservation law over the membership map stays green after each move (architecture-rules, backend-tests).
```

#### E7

##### Promises

P7

```text
Nothing user-, client-, tracker- or database-visible changes: platform-answers-compare reports IDENTICAL answers (system info, version, encryption/secrets state, database-management status, usage-data state, log level and problems, update status); refresh-reactions-compare IDENTICAL; ef-model-unchanged shows no model change; every live suite passes on the move push (ci-live-suites-show); the full filtered backend suite is green (backend-tests). Program.cs-only-usings and no config key, key-store directory, Redis channel or hub route change are not directly provable by a finite probe and go to candidate review.
```

## Charter

None.

## Examination

None.
