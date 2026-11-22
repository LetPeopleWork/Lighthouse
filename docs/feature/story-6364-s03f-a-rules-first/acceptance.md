# Acceptance brief: story\-6364\-s03f\-a\-rules\-first

## Slices

None.

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
