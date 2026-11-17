# Product brief: bug\-6654\-restore\-open\-database

## Request

```text
Bug 6654: Restore and Clear Database fail on Windows with SQLite ("file is being used by another process"). On Windows (and macOS) with the SQLite provider, Database Management -> Restore Backup always fails with System.IO.IOException: The process cannot access the file LighthouseAppContext.db because it is being used by another process. Clear Database fails the same way. Additional request: warn that backups do not include the encryption key, check before restoring, and allow starting anyway; maintainer decision: allow restoring without the unreadable credentials so the app starts and the user re-enters them.
```

## Work

M

```text
An operator can restore a backup or clear the database on any OS, and a backup from another installation never leaves Lighthouse unable to start.
```

### Rework

None.

## Promises

### P1

Outcome

```text
Restore Backup and Clear Database complete on the SQLite provider while the running application holds the database file open, on Windows, macOS and Linux; afterwards the application reads the restored (or empty, re-seeded) data.
```

### P2

Outcome

```text
Before a restore changes any data, Lighthouse checks whether its encryption key can read the stored credentials in the backup; when it can read none of them, the restore stops with a clear message and nothing is changed. The Backup and Restore sections say that a backup does not contain the encryption key.
```

### P3

Outcome

```text
When the check finds no readable credential, the user can choose Restore without credentials: the restore completes, the unreadable credentials are removed, the application starts, and the user enters each token again in its Connection.
```

## Scope

None.

```text
Letting EF own the SQLite connection (DatabaseConfigurator contextOwnsConnection) is a separate issue, unless the design needs it for P1.
```

## Decisions

```text
Maintainer 2026-11-17: rigor profile thorough for all slices (opus crafter/acceptance designer/architect, sonnet reviewers).
```

#### Status

Confirmed


```text
Maintainer 2026-11-17: key-warning request is included in this bug; slices S1-S3 and the UI sketch (info Alert on Backup, new line on Restore, confirmation Dialog reusing the Clear-dialog pattern with Cancel / Restore without credentials) approved.
```

#### Status

Confirmed


```text
The pre-check stops a restore only when the backup holds stored credentials and none can be read (the same NothingReadable case that refuses startup); partially readable backups restore as before.
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
A restore of a backup whose credentials this installation cannot read stops before changing anything, with a clear message; Backup and Restore show the key warning
```

#### Promises

P2

#### Depends on

S3

#### Slice work

M

```text
A backup from another installation never leaves Lighthouse unable to start by surprise
```

##### Rework

None.

### S2

```text
From that message the user restores without credentials; the restore completes, the unreadable credentials are gone and the app starts
```

#### Promises

P3

#### Depends on

S1

#### Slice work

M

```text
Configuration can be moved between installations at the cost of re-entering tokens
```

##### Rework

None.

### S3

```text
Restore Backup and Clear Database succeed on SQLite while the application holds the database file open
```

#### Promises

P1

#### Depends on

None.

#### Slice work

S

```text
Restore and Clear work on Windows and macOS
```

##### Rework

None.
