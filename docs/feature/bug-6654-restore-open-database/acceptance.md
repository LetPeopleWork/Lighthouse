# Acceptance brief: bug\-6654\-restore\-open\-database

## Slices

### S3

##### Covers

P1

```text
While the application keeps its database open and another handle holds the file, the operator restores a backup made earlier (one whose rows are all in its database file, and one whose newest rows are only in its write-ahead log), after a further blackout period was added
```

```text
Restore completes, and the connection the application opened before the restore reads exactly the backup's blackout periods, without the one added after the backup
```

```text
On Windows, macOS and Linux, Restore Backup succeeds while Lighthouse runs and Lighthouse then shows the backup's data
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs\:\:Restoring\_a\_backup\_while\_the\_application\_holds\_the\_database\_open\_shows\_exactly\_the\_backups\_data

```text
A restore that wrote the backup's rows but left rows from the live database's own write-ahead log visible to a connection opened later than the scenario's (only rows read through the already-open connection are checked)
```

###### Obligations



##### Covers

P1

```text
While the application keeps its database open, a blackout period is stored and the operator clears the database
```

```text
Clear completes, and the connection the application opened before the clear no longer sees the blackout period and sees the seeded terminology
```

```text
On Windows, macOS and Linux, Clear Database succeeds while Lighthouse runs and Lighthouse then shows an empty, re-seeded database
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs\:\:Clearing\_the\_database\_while\_the\_application\_holds\_it\_open\_shows\_an\_empty\_seeded\_database

```text
A clear that emptied only the tables the scenario checks (blackout periods) and left other user data in place
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
On SQLite, while the application is running, I run Restore Backup and it completes without a file-in-use error, on Windows, macOS and Linux.
```

#### E2

##### Promises

P1

```text
On SQLite, while the application is running, I run Clear Database and it completes without a file-in-use error, on Windows, macOS and Linux.
```

#### E3

##### Promises

P1

```text
After a restore the application shows the data from the backup; after Clear Database it shows an empty, re-seeded database; no stale pre-operation data remains.
```

#### E4

##### Promises

P2

```text
When I start a restore and Lighthouse cannot read any stored credential in the backup with its current encryption key, the restore stops before changing anything, shows a clear message, and my existing data is unchanged.
```

#### E5

##### Promises

P2

```text
The Backup and Restore sections of the documentation and UI state that a backup does not contain the encryption key.
```

#### E6

##### Promises

P2

```text
When the key can read at least one credential in the backup, the restore is not blocked by the check.
```

#### E7

##### Promises

P3

```text
When the check finds no readable credential, I am offered Restore without credentials, and choosing it completes the restore with the backup data restored.
```

#### E8

##### Promises

P3

```text
After Restore without credentials, the unreadable credentials are gone, the application starts normally, and I can enter each token again in its Connection.
```

## Charter

None.

## Examination

None.
