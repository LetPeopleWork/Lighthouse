# Acceptance brief: bug\-6654\-restore\-open\-database

## Slices

### S1

##### Covers

P2

```text
The operator opens Settings, Database Management.
```

```text
The Backup section shows the note that the backup does not contain this installation's encryption key and its stored credentials can only be read by an installation that uses the same key; the Restore section says Lighthouse checks, before it changes anything, that this installation's key can read the backup's stored credentials.
```

```text
Before backing up or restoring, the operator knows the key is not in the backup and that a restore checks it first.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.test\.tsx\:\:says in Backup and Restore that a backup does not carry the encryption key and is checked before restoring

```text
The note shown as plain text instead of an info alert would still pass; only the approved words are pinned.
```

###### Obligations

None.

##### Covers

P2

```text
The operator chooses a backup file, enters its password and restores; the restore answers Failed with the code CredentialsUnreadable; the operator clicks Cancel.
```

```text
A dialog titled Stored credentials in this backup cannot be read shows the approved explanation; Cancel closes it and the chosen file and password stay filled in.
```

```text
The operator understands why the restore stopped and can close the message without losing the file and password they entered.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.test\.tsx\:\:explains that the backup\'s credentials cannot be read\, and Cancel closes the explanation keeping the chosen file and password

```text
A dialog that also shows the generic error alert behind it would still pass.
```

###### Obligations

None.

##### Covers

P2

```text
The operator restores a backup and the restore answers Failed with the same reason text but without a failure code.
```

```text
The reason shows as the existing error, and no credentials dialog opens.
```

```text
Other restore failures look as they always did; the credentials explanation appears only for unreadable credentials.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.test\.tsx\:\:shows a failed restore without the unreadable\-credentials code as an error\, not as the credentials explanation

```text
A dialog keyed on a failure code other than CredentialsUnreadable would pass this case; the previous case pins the code that opens it.
```

###### Obligations

None.

##### Covers

P2

```text
The operator restores a backup that holds no credentials, or one credential this installation's key can read and one it cannot, or only credentials it can read, after a blackout period was added.
```

```text
Each restore ends Completed and the live database holds exactly the backup's blackout periods.
```

```text
Backups that the next start accepts restore as before; only a backup whose credentials cannot be read at all is stopped.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs\:\:Restoring\_a\_backup\_with\_no\_credentials\_or\_at\_least\_one\_this\_key\_can\_read\_restores\_the\_backup

```text
A restore that completes but keeps the live rows would pass a state-only check; the blackout-period comparison catches it.
```

###### Obligations




##### Covers

P2

```text
On PostgreSQL, a backup is checked and the check is then finished.
```

```text
A scratch database named after the live one with _restore_check is dropped and created, only the connection-option and OAuth credential tables are restored into it, and it is dropped again when the check is finished; nothing else runs.
```

```text
Checking a PostgreSQL backup leaves no extra database behind and never touches the live one.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/PostgresDatabaseManagementProviderTest\.cs\:\:Checking\_a\_backup\_restores\_only\_the\_credential\_tables\_into\_a\_scratch\_database\_and\_drops\_it\_afterwards

```text
A check that restores further tables in the same pg_restore call would still pass; the argument list is pinned only to the two table names and the scratch database.
```

###### Obligations


##### Covers

P2

```text
Credentials stored under one installation's key are backed up, the installation then runs on another key that reads none of them, a blackout period is added, and the operator restores the backup with its password.
```

```text
The restore ends Failed with the code CredentialsUnreadable, sent as text, and the reason that none of the backup's credentials can be read and nothing has been changed; the live database holds the same rows as before, nothing was written to it and its file did not change; another backup can be made right after.
```

```text
The operator is told the backup's credentials cannot be read and finds Lighthouse exactly as it was, still able to start.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs\:\:Restoring\_a\_backup\_whose\_credentials\_this\_key\_cannot\_read\_stops\_and\_changes\_nothing

```text
A check that stops on any credential it cannot read, even when another is readable, still passes this case; the mixed-credentials case catches it.
```

###### Obligations



##### Covers

P2

```text
On PostgreSQL, a backup is checked and pg_restore fails.
```

```text
The check fails with an error, the scratch database is still dropped, and no command drops or creates the live database.
```

```text
A PostgreSQL backup that cannot be checked stops the restore before the live database is dropped, and leaves nothing behind.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/PostgresDatabaseManagementProviderTest\.cs\:\:Checking\_a\_backup\_that\_pg\_restore\_cannot\_read\_fails\_drops\_the\_scratch\_database\_and\_never\_drops\_the\_live\_one

```text
A provider that drops the scratch database only on failure but not after a successful check passes this case; the success case catches it.
```

###### Obligations


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
