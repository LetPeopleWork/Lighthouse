# Acceptance brief: story\-6364\-s03f\-b\-move

## Slices

None.

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
