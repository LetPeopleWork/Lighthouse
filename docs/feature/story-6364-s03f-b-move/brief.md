# Product brief: story\-6364\-s03f\-b\-move

## Request

```text
ADO Story #6364 S03f Platform composition root, feature B (the move): the key ring and secret custody live in Platform/Encryption (B1); database management, demo data, the seeder port and the race-tolerant save live in Platform/ (B2); usage data, logging and releases live in Platform/ (B3); the dispatcher and its contracts, the clock and the host facilities live in Platform/ (B4); the update queue's mechanics live in Platform/UpdateQueue while the pipelines stay (B5). Pure moves: behaviour unchanged, proven by the feature A probes. Builds on feature A's subject-aware preservation law (confirmed by the maintainer 2026-11-22). DESIGN and DEVOPS: docs/feature/story-6364-s03f-platform-composition-root/feature-delta.md.
```

## Work

L

```text
The platform types live in Platform/<Area>, so the platform lane can be ruled by namespace
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every type that feature A's membership map (PlatformLaneMembership) tags B1 - the key ring, secret custody, CryptoService, the key-ring file watcher and the admitted Models/Encryption types - lives under Platform/Encryption, namespace equal to folder, and none of them is left in its old namespace; test-time-budget.json and its Python test name the four Encryption test classes by their new full names, so the budget check still finds them.
```

### P2

Outcome

```text
Every type the membership map tags B2 lives under Platform/DatabaseManagement, Platform/DemoData, Platform/Seeding (ISeeder and InstallTimestampSeeder) or Platform/Persistence (LostRaceTolerantSave), namespace equal to folder, none left in its old namespace; the seven context seeders stay in Services/Implementation/Seeding and reach ISeeder in its new place; the GlobalSuppressions.cs target of PostgresDatabaseManagementProvider names its new full name, so dotnet build stays clean.
```

### P3

Outcome

```text
Every type the membership map tags B3 lives under Platform/UsageData, Platform/Logging or Platform/Releases, namespace equal to folder, none left in its old namespace; the usage-data pipe sends the same events (no UsageDataEventName added); the GlobalSuppressions.cs target of LighthouseReleaseService names its new full name; a change under Platform/Releases still selects the GitHub live suite.
```

### P4

Outcome

```text
Every type the membership map tags B4 - the domain-event dispatcher with IDomainEventDispatcher and IDomainEventHandler<T>, the clock, RandomNumberService and the other host facilities - lives under Platform/DomainEvents or Platform/Host, namespace equal to folder, none left in its old namespace; the domain-event handlers stay with their contexts; the HostUsersStillToMove entries and the CalendarDayAnchorSeam path name the moved tests in their new place; the GlobalSuppressions.cs target of RandomNumberService.GetRandomNumber names its new full name.
```

### P5

Outcome

```text
Every update-queue mechanics type the membership map tags B5 lives under Platform/UpdateQueue, namespace equal to folder, none left in its old namespace; TeamUpdater, PortfolioUpdater, UpdateServiceBase<T> and their three interfaces did not move.
```

### P6

Outcome

```text
The unit tests of the moved types live under Tests/Platform/<Area> (namespace equal to folder); every architecture rule and list that names a moved type names it in its new place, and feature A's subject-aware preservation law stays green over the membership map after each move.
```

### P7

Outcome

```text
Nothing a user, an API client, a tracker or the database sees changes: platform-answers-compare reports the same answers (system info, version, encryption and secrets state, database-management status, usage-data state, log level and problems, update status) before and after; refresh-reactions-compare shows the domain-event reactions in the same order; the EF model is unchanged; Program.cs changes only its usings; no configuration key, key-store directory, Redis channel or hub route changes; every live suite passes on the move push and the full filtered backend suite stays green.
```

## Scope

```text
Pure moves of the platform production types measured in feature A's membership map into Platform/<Area> (Encryption, DatabaseManagement, DemoData, Seeding, Persistence, UsageData, Logging, Releases, DomainEvents, Host, UpdateQueue; namespace equal to folder), the unit tests of the moved types into Tests/Platform/<Area>, and the usings, rule and list strings, test-time budget keys, HostUsersStillToMove entries, test paths and GlobalSuppressions.cs targets that follow; behaviour unchanged.
```

```text
The platform lane's own rules and the unit-test placement rule (feature C); the membership map, the preservation law, the GitHub classifier and platform-answers-compare (feature A, delivered); Data and its migrations, every repository with IRepository<T> and RepositoryBase<T>, the seven context seeders, the update pipelines (TeamUpdater, PortfolioUpdater, UpdateServiceBase<T> and their interfaces), the domain-event handlers, the host band (Startup, Health, Configuration, Standalone, Cache, Extensions, Factories), TestData zip fixtures and the Models entity and wire types (they stay); legalising the dispatcher contracts and clock by namespace for every module (own Story); any schema, configuration, signature or behaviour change; mutation and the finalize checklist (run after feature C, not nwdes slices). Nothing a user sees changes, so there is no UI sketch and no usage-data event.
```

## Decisions

```text
The moves build on feature A's subject-aware preservation law with PlatformLanePatternRoles: Context patterns widen by whole Platform area, Place patterns gain nothing (maintainer 2026-11-22).
```

#### Status

Confirmed


```text
One commit per slice (tests moved in their own refactor commit); B1-B5 are pushed together once all five are green locally (fast loop and full filtered gate), so one full live run covers the whole move (D-12 option b; AFK default of open question 4, maintainer not yet asked).
```

#### Status

Proposed


```text
Rigor: B1 (key ring and secret custody) and B5 (update queue locks, Redis, Postgres) thorough (opus acceptance designer and crafter, sonnet reviewers); B2, B3 and B4 standard (sonnet acceptance designer and crafter, haiku reviewers); EXAMINE uses the reviewer model (open question 8, AFK default taken).
```

#### Status

Proposed


```text
Layout Platform/<Area> with 11 areas and no layers (D-1); Data and all repositories stay (D-0, D-4); the update pipelines stay (D-6); the dispatcher contracts and the clock stay listed by full name in each module's list, renamed (D-7, open question 5); the host band stays where it is (open question 6); Program.cs changes only usings (D-11). AFK defaults of open questions 2, 3, 5 and 6.
```

#### Status

Proposed


```text
Each slice RED is structural (its layout test fails while the types sit in their old namespaces); behaviour is proved by the unchanged tests and EXAMINE through admitted probes only: architecture-rules Platform and all, ef-model-unchanged, platform-answers-compare, refresh-reactions-compare, test-selection-show, ci-live-suites-show, backend-tests (D-13).
```

#### Status

Proposed


```text
B4 also tries, in a separate edit commit, to move DomainEventDispatcherGoldTest and LighthouseClockTest off a host (improve the tests you touch); if a day is not enough it records why and the rows stay on HostUsersStillToMove (D-10).
```

#### Status

Proposed


## Journey

None.

## Slices

### S1

```text
B1: the key ring and secret custody live in Platform/Encryption, with their tests in Tests/Platform/Encryption; the test-time budget keys and R-T6 strings follow.
```

#### Promises

P1

P6

P7

#### Depends on

None.

#### Slice work

M

```text
The first platform area exists under Platform/, so the key ring and secret custody are ruled by namespace
```

##### Rework

None.

### S2

```text
B2: database management, demo data, the seeder port and the race-tolerant save live in Platform/DatabaseManagement, DemoData, Seeding and Persistence; the context seeders stay and the Postgres provider suppression follows.
```

#### Promises

P2

P6

P7

#### Depends on

S1

#### Slice work

S

```text
Database management, demo data, seeding and the race-tolerant save are ruled by namespace
```

##### Rework

None.

### S3

```text
B3: usage data, logging and releases live in Platform/UsageData, Logging and Releases; the release service suppression follows and the GitHub live suite is still selected for the release code.
```

#### Promises

P3

P6

P7

#### Depends on

S1

#### Slice work

S

```text
Usage data, logging and releases are ruled by namespace
```

##### Rework

None.

### S4

```text
B4: the domain-event dispatcher and its two contracts, the clock and the host facilities live in Platform/DomainEvents and Platform/Host; the handlers stay with their contexts, and HostUsersStillToMove, the CalendarDayAnchorSeam path and the RandomNumberService suppression follow.
```

#### Promises

P4

P6

P7

#### Depends on

S1

#### Slice work

M

```text
The dispatcher, the clock and the host facilities are ruled by namespace
```

##### Rework

None.

### S5

```text
B5: the update queue's mechanics live in Platform/UpdateQueue while TeamUpdater, PortfolioUpdater, UpdateServiceBase<T> and their interfaces stay; the Forecasting and Work Items list strings and placement-test paths follow.
```

#### Promises

P5

P6

P7

#### Depends on

S1

#### Slice work

M

```text
The update queue's mechanics are ruled by namespace, separate from the refresh pipelines
```

##### Rework

None.
