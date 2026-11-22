# Product brief: story\-6364\-s03f\-a\-rules\-first

## Request

```text
ADO Story #6364 S03f Platform composition root, feature A (rules first): the platform lane's membership is measured from the IL and fixed (A1); every rule that sees a moved platform type today still sees it after the move, and no rule string names a moving area by its old path (A2); a change to the release code under Platform/Releases still selects the GitHub live suite (A3); an answers probe for the platform's endpoints exists before any file moves and reports a changed answer (A4). Rules, scripts and probes only: production behaviour unchanged. DESIGN and DEVOPS: docs/feature/story-6364-s03f-platform-composition-root/feature-delta.md.
```

## Work

M

```text
The platform types can move to Platform/ without any rule, CI selection or answer silently changing
```

### Rework

None.

## Promises

### P1

Outcome

```text
The membership of the platform lane is measured from the compiled code and fixed in a test: each platform type with its area, its old and its new namespace, and the files that stay with their owner; the test finds each listed type in its old or its new namespace, fails when a listed type is missing or a listed name does not exist, finds no domain-event handler outside Modules, and records every suppression or analyser-config target that names a moving type.
```

### P2

Outcome

```text
Every rule pattern in the test assembly gives the same answer for each moving platform type under its new Platform namespace as under its old one, and no added alternative matches a production type outside Platform; the inline DomainEvents fragment is replaced by the exact dispatcher type.
```

### P3

Outcome

```text
No string in the rule files names a moving area (Encryption, DatabaseManagement, UsageData, Logging, DomainEvents, BackgroundServices.Update, Interfaces.Update) or a moving loose type by its old Services.Implementation, Services.Interfaces or Services/Implementation path, except the entries the membership list names as renamed by a move slice.
```

### P4

Outcome

```text
A change to Platform/Releases/GitHubService.cs, or to its test path, selects the GitHub live suite in CI test selection, and every path the classifier already handled keeps its selection.
```

### P5

Outcome

```text
A platform-answers-compare examine command, declared in .nwave/examine.json before any file moves, compares the platform answers of two revisions (system info, version without the GitHub calls, encryption and secrets state, database-management status, usage-data state, log level and problems, update status; volatile fields dropped) and says DIFFERENT with exit 1 when one answer changed, IDENTICAL with exit 0 when none did, and fails when nothing was compared.
```

### P6

Outcome

```text
Nothing a user, a tracker or the database sees changes: no production file moves or changes behaviour in feature A, and every existing test stays green.
```

## Scope

```text
The IL-measured platform lane membership map (types, areas, old and new namespaces, files that stay with owner, suppression and analyser-config targets naming moving types); the preservation law over every const regex in the test assembly with the widening it requires, the dispatcher fragment made exact, and the old-path literal check; the GitHub live-suite selection for Platform/Releases in path-classifier.sh; the platform-answers-compare examine command and its declaration in .nwave/examine.json.
```

```text
Moving any platform file or test, and renaming the list and path strings that name a moved type (feature B, B1-B5); the platform lane rules R-PL1 to R-PL4 and unit-test placement (feature C, C1-C2); Data, migrations and every repository (stay, D-0 and D-4); the host band Startup, Health, Configuration, Standalone, Cache, Extensions, Factories (stays); context seeders and update pipelines (stay); an AddPlatform extension (S09a); inverting DatabaseMaintenanceGate to Flow metrics (improvement item, not filed); Redis and Postgres update locks under a real cluster and a key ring on a mounted file (left to container and unit tests, not to the answers probe).
```

## Decisions

```text
Rigor thorough for feature A: architect, acceptance designer and crafter on opus; reviewers and EXAMINE on sonnet (DESIGN open question 8, AFK default taken; maintainer asleep, root context 2026-11-22).
```

#### Status

Confirmed


```text
S03f starts only after S03e (Story 6363) is closed, which it is; size L for the Story, about 10-11 days, 11 slices in three features (A 4, B 5, C 2), no split of the ADO Story; no review window (behaviour-free); UI sketch, loading and failure looks N/A; usage-data event N/A because nothing a user sees or does changes (DESIGN and DEVOPS record, AFK defaults taken).
```

#### Status

Confirmed


```text
EXAMINE observes through admitted probes only: architecture-rules (Platform and all), ef-model-unchanged, platform-answers-compare once A4 adds it, refresh-reactions-compare, test-selection-show, ci-live-suites-show, backend-tests (D-13).
```

#### Status

Confirmed


```text
Quint scenario exploration not relevant to any value of feature A: no state, ordering, concurrency, recovery or liveness claim changes; the preservation law is decided exhaustively over a finite domain by an executable check, so a model would restate the namespace map and could not fail (DESIGN formal verification).
```

#### Status

Confirmed


```text
Order: A1 before A2, because the preservation law runs over the membership map; A3 lands before B3 so the GitHub suite is selected on the move push; A4 before any file moves. The endpoints that answer without network and their volatile fields are measured in A4; the full list of patterns the law finds is measured in A2.
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
A1: the platform lane membership is measured from the IL and fixed in a namespace-agnostic test that fails when a listed type is missing or a listed name does not exist, finds no domain-event handler outside Modules, and records the suppression and analyser-config targets naming moving types.
```

#### Promises

P1

P6

#### Depends on

None.

#### Slice work

S

```text
The move in feature B has a fixed, measured list of what moves, where, and what stays with which owner
```

##### Rework

None.

### S2

```text
A2: every rule pattern gives the same answer for each moving platform type under its old and its new namespace, no added alternative matches a production type outside Platform, and no rule string names a moving area by its old path.
```

#### Promises

P2

P3

P6

#### Depends on

S1

#### Slice work

M

```text
No rule silently stops seeing a platform type when it moves
```

##### Rework

None.

### S3

```text
A3: a change to Platform/Releases/GitHubService.cs or its test path selects the GitHub live suite, and the existing classifier cases keep their selection.
```

#### Promises

P4

P6

#### Depends on

None.

#### Slice work

S

```text
The live GitHub suite still runs when the release code moves
```

##### Rework

None.

### S4

```text
A4: the platform-answers-compare examine command says DIFFERENT (exit 1) for two captures differing in one answer, IDENTICAL (exit 0) for identical ones, and fails when nothing was compared.
```

#### Promises

P5

P6

#### Depends on

None.

#### Slice work

S

```text
A changed platform answer after the move is caught at EXAMINE
```

##### Rework

None.
