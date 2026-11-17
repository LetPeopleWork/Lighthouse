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


### S2

##### Covers

P3

```text
A backup holds a board connection with a token and a plain option, and an OAuth connection, all saved under the key of another installation; the operator restores it on this installation and chooses to restore without credentials.
```

```text
The restore completes; the start-up check over the restored database finds no stored credential, so the next start does not refuse; both connections and the plain option are there, the token is empty and the OAuth credential is gone; the backup's data replaced what was added after it.
```

```text
The restore finishes, Lighthouse starts, every Connection is still there and asks for its token again.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs\:\:Restoring\_without\_credentials\_completes\_removes\_the\_unreadable\_credentials\_and\_lets\_the\_application\_start

```text
A removal that runs after migrations and seeding instead of before them would still pass; the order is not visible at the port.
```

###### Obligations


##### Covers

P3

```text
The operator chooses to restore without credentials a backup that holds no credential, or one credential this installation can read and one it cannot.
```

```text
The restore completes and the restored database equals the backup row for row: nothing is removed.
```

```text
A backup that the next start would not refuse is restored exactly as it was, including every credential.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs\:\:Restoring\_without\_credentials\_a\_backup\_with\_no\_credentials\_or\_one\_this\_key\_can\_read\_restores\_it\_unchanged

```text
Removal that runs when the backup holds a readable credential but leaves values untouched by luck (for example a removal that only blanks values of connections with no readable secret) would still pass; the row pins only the two cases DESIGN names.
```

###### Obligations



##### Covers

P3

```text
The live database holds an unreadable token, a readable token, an empty secret value, plain options, an OAuth credential whose access token is readable but refresh token is not, one with both tokens unreadable and one with both readable; the unreadable credentials are removed with this installation's key.
```

```text
Only the unreadable token is blanked; the readable token and the empty value are untouched; plain options stay; both OAuth credentials with any unreadable token are deleted and the fully readable one stays.
```

```text
Only credentials Lighthouse cannot read are taken away; everything it can read stays in place.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs\:\:Removing\_unreadable\_credentials\_blanks\_or\_deletes\_only\_what\_this\_key\_cannot\_read

```text
A removal that deletes the empty secret option row instead of leaving it, then re-creates one with an empty value, would still pass; the empty case cannot tell blanking from leaving alone.
```

###### Obligations





##### Covers

P3

```text
The settings page sends a restore with the form field removeUnreadableCredentials set to true, set to false, or absent.
```

```text
The service is asked to remove unreadable credentials only for true; for false or an absent field it is asked to stop the restore, as before; the request is accepted.
```

```text
Credentials are removed only when the operator chose Restore without credentials.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DatabaseManagementControllerTest\.cs\:\:RestoreBackup\_PassesTheOperatorsChoiceAboutUnreadableCredentialsToTheService

```text
A controller that reads the field from the request form and also from a parameter, preferring the wrong one when they disagree, would still pass because the test sets both alike.
```

###### Obligations


##### Covers

P3

```text
The settings page asks the client to restore a backup with the choice to remove unreadable credentials, then without it.
```

```text
The first request carries removeUnreadableCredentials true with the same file and password; the second carries no such field.
```

```text
The server learns of the operator's choice only when it was made.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/services\/Api\/DatabaseManagementService\.test\.ts\:\:sends the choice to restore without unreadable credentials only when it is made

```text
A client that sends the field as 1 instead of true would fail; a client that sends false when not chosen would fail; a client sending true but to a different route would still pass this assertion (the route is pinned by the existing restore row).
```

###### Obligations


##### Covers

P3

```text
After the restore stopped because no credential could be read, the operator clicks Restore without credentials in the explanation dialog.
```

```text
The second restore is sent with the same file and password and the choice set; the button shows it is busy while it runs; then the dialog closes and the success message Restore completed successfully. Your data has been restored. shows.
```

```text
One click from the explanation finishes the restore and the page says it succeeded.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.test\.tsx\:\:restores without credentials from the explanation\, showing it is busy\, then closes it and reports success

```text
A dialog that closes before the second request ends, with the button disabled for another reason, would fail the busy check only if it closes first; a button that is disabled but shows no spinner would still pass.
```

###### Obligations


##### Covers

P3

```text
The operator clicks Restore without credentials and the second restore request fails.
```

```text
The error message from the failed request shows in the existing error alert.
```

```text
A failed restore without credentials is reported, never shown as success.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.test\.tsx\:\:shows a failed restore without credentials as an error

```text
Showing the error inside the dialog instead of the page's error alert would still pass, since the test finds the text anywhere on the page.
```

###### Obligations


##### Covers

P3

```text
The source of the secret read path is inspected after the removal and staged-backup code is added.
```

```text
The classifier still has no catch clause or exception filter, and no code outside the encryption code catches a cryptographic failure.
```

```text
An unreadable credential is never reported as a working one because a failure was swallowed.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretReadPathSourceStructureTest\.cs\:\:NothingOutsideTheEncryptionCode\_CatchesACryptographicFailure

```text
A catch of a general exception (not a cryptographic one) inside the new removal type in the encryption folder would still pass; the existing law pins only the classifier, the envelope and cryptographic catches outside that folder. Already satisfied, so it passes now, no RED.
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
