# Architecture brief: bug\-6654\-restore\-open\-database

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
SQLite restore and clear write through the SQLite online backup API (Microsoft.Data.Sqlite SqliteConnection.BackupDatabase) instead of replacing or deleting files. Restore: source is a Pooling=False connection on the staged backup .db in the extract directory (SQLite applies a legacy backup's -wal when it opens it); destination is a new Pooling=False connection on the live connection string with PRAGMA busy_timeout=10000. Clear: source is an empty Data Source=:memory: connection. The live .db, -wal and -shm files are never replaced or deleted, so every open connection, pooled or leaked, reads the new pages, and journal_mode stays WAL. RecycleConnection, migrations and seeding still follow in DatabaseManagementService. Owner: SqliteDatabaseManagementProvider.
```
Governed slices: S3

#### D2

```text
Out of scope, unchanged: CreateBackup keeps its file copy (it opens the live file with FileShare.ReadWrite and already works on Windows; P1 does not need it), and DatabaseConfigurator keeps creating its own SqliteConnection (letting EF own it is not needed for P1, because the backup API never needs the file handles closed).
```
Governed slices: S3

#### D3

```text
Postgres staging: PostgresDatabaseManagementProvider.StageBackup creates a scratch database named after the live one with the suffix _restore_check (psql CREATE DATABASE, after DROP DATABASE IF EXISTS WITH (FORCE)), runs pg_restore --table=WorkTrackingSystemConnectionOption --table=OAuthCredentials into it, and its IStagedBackup opens an NpgsqlConnection to it and drops it WITH (FORCE) on DisposeAsync. Only the two credential tables are restored, so the check costs a fraction of a full restore. A staging failure throws and the restore fails before the live database is touched. Rejected: checking after the restore (breaks nothing-is-changed), parsing pg_restore SQL output (COPY escaping is brittle), skipping the check on Postgres (breaks P2).
```
Governed slices: S1

#### D4

```text
SQLite staging: SqliteDatabaseManagementProvider.StageBackup returns an IStagedBackup over the extracted .db (the same file RestoreBackup copies from) whose OpenConnection returns a Pooling=False SqliteConnection; DisposeAsync does nothing, because DatabaseManagementService already deletes the temporary directory.
```
Governed slices: S1

#### D5

```text
Request contract: IDatabaseManagementService.RestoreBackup(Stream backupFile, string password, UnreadableCredentials onUnreadable) with the closed enum UnreadableCredentials { StopRestore, Remove }. DatabaseManagementController.RestoreBackup reads an optional form field removeUnreadableCredentials (bool, absent means false) and maps true to Remove. The frontend DatabaseManagementService.restoreBackup(file, password, removeUnreadableCredentials = false) appends the field only when true.
```
Governed slices: S2

#### D6

```text
Removal: only when onUnreadable is Remove and the pre-check answered NothingReadable, DatabaseManagementService calls, after provider.RestoreBackup and before migrations and seeding, new UnreadableStoredSecrets(ring).RemoveFrom(provider.CreateConnection()). It sets Value to the empty string on every WorkTrackingSystemConnectionOption row with IsSecret whose value SecretStateClassifier calls Unreadable, and deletes every OAuthCredentials row whose AccessToken or RefreshToken is Unreadable. Connections, non-secret options and all other data stay. Afterwards the startup probe answers NothingStored or SomethingReadable, so EncryptionKeyRingBootstrapper does not refuse; the Connection shows its token field empty and the user enters it again (an OAuth connection shows it is no longer authorised). With Remove and any other finding nothing is removed.
```
Governed slices: S2

#### D7

```text
Credential pre-check: DatabaseManagementService.RestoreBackup, after unzipping and before IDatabaseManagementProvider.RestoreBackup, asks the provider to stage the backup (await using IStagedBackup staged = await provider.StageBackup(extractDir)) and runs the existing DatabaseSecretReadabilityProbe over staged.OpenConnection with the running key ring from IEncryptionKeyRingHolder.Current. Only StoredSecretReadability.NothingReadable stops the restore; NothingStored, SomethingReadable and CannotTell restore as before, which is the confirmed maintainer decision and the same predicate the startup refusal in EncryptionKeyRingBootstrapper uses. The stop happens before the provider is called, so on Postgres it also happens before the provider drops the database.
```
Governed slices: S1, S2

#### D8

```text
Result contract: DatabaseOperationStatus gains a trailing optional DatabaseOperationFailure? FailureCode = null, with the closed enum DatabaseOperationFailure { CredentialsUnreadable }, serialised as a string like the other enums. DatabaseOperationTracker.TransitionToFailed gains an overload taking the code. A stopped restore is State Failed, FailureCode CredentialsUnreadable and FailureReason: None of the stored credentials in this backup can be read with this installation's encryption key. Nothing has been changed. The frontend opens its dialog on failureCode, never on the reason text.
```
Governed slices: S1, S2

#### D9

```text
Carriers and owners: decision carrier StoredSecretFinding (existing, Encryption) is the only input of the stop decision; error carrier DatabaseOperationFailure in DatabaseOperationStatus (Services/Interfaces/DatabaseManagement); opt-in carrier UnreadableCredentials (Services/Interfaces/DatabaseManagement); staged-backup effect carrier IStagedBackup : IAsyncDisposable (Services/Interfaces/DatabaseManagement), produced per provider; removal owner UnreadableStoredSecrets (Services/Implementation/Encryption) holding the ring and deciding per stored value; the set of stored-secret columns is owned once by StoredSecretLocations (Services/Implementation/Encryption), read by both the probe and the removal. Per-process state: no new state; DatabaseOperationTracker keeps the status and DatabaseMaintenanceGate keeps the single active operation.
```
Governed slices: S1, S2

#### D10

```text
Frontend (System, Database tab, DatabaseManagementSettings): the Backup section gets an info Alert with the approved key-warning text, the Restore section a new Typography line with the approved check text, and a Dialog that reuses the Clear Database dialog pattern (Dialog, DialogTitle, DialogContent, DialogActions) opens when the restore result has failureCode CredentialsUnreadable. S1 ships the dialog with Cancel, which closes it and keeps the chosen file and password. S2 adds Restore without credentials, which re-sends the same file and password with removeUnreadableCredentials true, shows the button's loading state while it runs and closes the dialog; a failed second request shows the existing error Alert. No view fetches new data, so the loading look is the existing button loading state and the failure look the existing error Alert.
```
Governed slices: S1, S2

#### D11

```text
Checker mapping: the restore, check, copy and removal state machine (P2, P3 safety) is a finite state model checked with Quint (quint verify on the Apalache backend, exhaustive at the reachable depth of 5 steps because every run settles within 3 transitions). TLA+/TLC is not needed: there is no temporal or fairness claim. Agda is not selected: no claim needs a proof beyond the finite model. The SQLite page-level behaviour behind P1 (an open connection reads the pages the backup API wrote) is a property of SQLite, not of this design, so it is not modelled; the S3 oracle observes it at runtime. Liveness is not applicable: each operation is one synchronous, bounded request that never waits on another party (gate acquisition returns at once), so no progress claim depends on fairness.
```
Governed slices: S1, S2

#### D12

```text
Context map: the change sits in the platform lane of ADR-270 (backups and restore, the encryption key ring); it reads and blanks Tracker-integration credential columns only inside a whole-database restore, as the startup probe already reads them, so no context, edge, domain event or glossary term changes. UI surface (ADR-271): System, Database tab, already its target.
```
Governed slices: S1, S2, S3

### Constraints

#### K1

```text
Inputs keep their existing bounds: the backup upload is limited to 512 MiB (RequestSizeLimit) and the password must be non-empty; removeUnreadableCredentials is an optional boolean with no other values. No new bound is introduced.
```

#### K2

```text
The destination connection of the backup API sets PRAGMA busy_timeout=10000, the same wait DatabaseConfigurator gives every application connection; no longer wait is introduced.
```
Governed slices: S3

#### K3

```text
No schema change and no EF migration (migrations are expand-only).
```

#### K4

```text
Postgres staging needs the CREATEDB privilege the provider already needs for ClearDatabase; the scratch database is always dropped, also on failure.
```
Governed slices: S1

#### K5

```text
Backend acceptance scenarios drive DatabaseManagementService directly under Lighthouse.Backend.Tests/Acceptance/DatabaseManagement, no host; that folder is named in the feature's Stryker test-case filter (Tests.Acceptance.DatabaseManagement). No nWave scenario tags in code. Comments are rare plain-language whys.
```

#### K6

```text
Backups made before this change (file copies with optional -wal and -shm) stay restorable and checkable.
```
Governed slices: S1, S2

### Reuse

DatabaseSecretReadabilityProbe

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/StoredSecretReadabilityProbe\.cs\:92

Reuse

```text
Already answers exactly the startup refusal question over any DbConnection factory; pointed at the staged backup it is the pre-check, so the check and the refusal cannot disagree.
```

SecretStateClassifier

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/SecretStateClassifier\.cs\:15

Reuse

```text
Sole owner of whether one stored value is readable; the removal asks it per value.
```

IEncryptionKeyRingHolder

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Encryption\/IEncryptionKeyRingHolder\.cs\:5

Reuse

```text
Singleton holding the running ring; Current is the key the restored database will be read with.
```

SqliteDatabaseManagementProvider

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs\:8

Extend

```text
Owns the SQLite file mechanics; restore and clear change mechanism, and it gains StageBackup and CreateConnection.
```

PostgresDatabaseManagementProvider

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/PostgresDatabaseManagementProvider\.cs\:9

Extend

```text
Owns pg_dump, pg_restore and psql; gains StageBackup over a scratch database and CreateConnection.
```

IDatabaseManagementProvider

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IDatabaseManagementProvider\.cs\:3

Extend

```text
Per-provider staging and connection creation belong on the provider port.
```

IDatabaseManagementService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IDatabaseManagementService\.cs\:3

Extend

```text
RestoreBackup gains the opt-in parameter.
```

DatabaseManagementService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:10

Extend

```text
Already sequences unzip, provider restore and migration behind the gate; check and removal slot into that sequence.
```

DatabaseOperationStatus

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/DatabaseOperationStatus\.cs\:3

Extend

```text
Existing result carrier gains an optional closed failure code; no second result type.
```

DatabaseOperationTracker

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseOperationTracker\.cs\:6

Extend

```text
Owns operation status; TransitionToFailed gains an overload with the code.
```

DatabaseManagementController

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs\:11

Extend

```text
Driving adapter; RestoreBackup reads the optional form field.
```

KeyThatReadsNothing

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/StoredSecretReadabilityProbe\.cs\:32

Reuse

```text
Kept unchanged: it addresses an operator at startup about environment variables; the shared knowledge is the NothingReadable predicate, reused through the probe.
```

SecretCustodyService

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/SecretCustodyService\.cs\:30

Reuse

```text
Considered for removal and left unchanged: it re-encrypts through EF on the running schema, while removal runs on a raw connection to a just-restored database before migrations.
```

DatabaseManagementSettings

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.tsx\:155

Extend

```text
Holds Backup, Restore and the Clear Dialog; the new dialog reuses that pattern and the existing Alert.
```

DatabaseManagementService

Lighthouse\.Frontend\/src\/services\/Api\/DatabaseManagementService\.ts\:21

Extend

```text
API client; restoreBackup gains the optional opt-in.
```

IDatabaseOperationStatus

Lighthouse\.Frontend\/src\/models\/DatabaseManagement\/DatabaseManagementTypes\.ts\:1

Extend

```text
Wire type gains failureCode.
```

StoredSecretLocations

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/StoredSecretLocations\.cs\:1

CreateNew

```text
The stored-secret queries are private to the probe; the removal needs the same set, so it moves to one owner both read.
```

UnreadableStoredSecrets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/UnreadableStoredSecrets\.cs\:1

CreateNew

```text
No existing type removes stored secrets; the classifier writes nothing and SecretCustodyService works through EF on the running schema.
```

IStagedBackup

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IStagedBackup\.cs\:1

CreateNew

```text
No type gives a readable, disposable view of a backup before restore; Postgres needs a scratch database with a lifetime.
```

DatabaseOperationFailure

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/DatabaseOperationFailure\.cs\:1

CreateNew

```text
No closed failure code exists; FailureReason is free text and must not be string-matched.
```

UnreadableCredentials

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/UnreadableCredentials\.cs\:1

CreateNew

```text
No opt-in carrier exists; a closed enum names the choice at the service port.
```

### Impact

None.

### Agreement

POST \/api\/latest\/database\-management\/restore\: multipart file\, password\, optional removeUnreadableCredentials\; response DatabaseOperationStatus with optional failureCode

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs\:58

Producer

Migrated

```text
Adds an optional form field and an optional response field; old clients that send neither get today's behaviour plus the stop.
```

POST \/api\/latest\/database\-management\/restore\: multipart file\, password\, optional removeUnreadableCredentials\; response DatabaseOperationStatus with optional failureCode

Lighthouse\.Frontend\/src\/services\/Api\/DatabaseManagementService\.ts\:54

Consumer

Migrated

```text
Sends the field on the second request and reads failureCode, in the same commit.
```

POST \/api\/latest\/database\-management\/restore\: multipart file\, password\, optional removeUnreadableCredentials\; response DatabaseOperationStatus with optional failureCode

Lighthouse\.Frontend\/src\/models\/DatabaseManagement\/DatabaseManagementTypes\.ts\:1

Consumer

Migrated

```text
Type gains failureCode as CredentialsUnreadable or null.
```

IDatabaseManagementProvider port\: RestoreBackup\, ClearDatabase\, StageBackup\, CreateConnection

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs\:8

Producer

Migrated

```text
Implements the new members and the new restore and clear mechanism.
```

IDatabaseManagementProvider port\: RestoreBackup\, ClearDatabase\, StageBackup\, CreateConnection

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/PostgresDatabaseManagementProvider\.cs\:9

Producer

Migrated

```text
Implements StageBackup and CreateConnection; restore and clear unchanged.
```

IDatabaseManagementProvider port\: RestoreBackup\, ClearDatabase\, StageBackup\, CreateConnection

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:10

Consumer

Migrated

```text
Calls StageBackup before RestoreBackup and CreateConnection for the removal.
```

IDatabaseManagementService\.RestoreBackup\(Stream\, string\, UnreadableCredentials\)

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:10

Producer

Migrated

```text
Takes the opt-in.
```

IDatabaseManagementService\.RestoreBackup\(Stream\, string\, UnreadableCredentials\)

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs\:58

Consumer

Migrated

```text
Passes Remove when the form field is true, StopRestore otherwise.
```

IDatabaseManagementService\.RestoreBackup\(Stream\, string\, UnreadableCredentials\)

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DatabaseManagementControllerTest\.cs\:152

Consumer

Migrated

```text
Moq setups name the new argument; the acceptance designer migrates them.
```

### Not applicable

contract_tests

```text
Producer and consumers of every changed contract (controller, frontend client, provider port, service port) ship in one deployable and change in the same commit; the slice oracles and the existing controller test cover them. No external client calls database management.
```

### Boundaries

```text
IDatabaseManagementService (RestoreBackup, ClearDatabase), driven by DatabaseManagementController and by acceptance scenarios
```

#### Driven ports

```text
IDatabaseManagementProvider (SQLite, Postgres): RestoreBackup, ClearDatabase, StageBackup, CreateConnection
```

```text
IStoredSecretReadabilityProbe over a DbConnection factory
```

```text
IEncryptionKeyRingHolder
```

```text
Controller -> IDatabaseManagementService -> DatabaseManagementService -> IDatabaseManagementProvider and Encryption (probe, classifier, UnreadableStoredSecrets); providers depend on Microsoft.Data.Sqlite, Npgsql and the command runner; nothing in Encryption depends on DatabaseManagement.
```

#### Failures

##### B1

```text
Staged backup holds stored credentials and none is readable, StopRestore
```

Refusal

```text
Failed, FailureCode CredentialsUnreadable, live unchanged
```

##### B2

```text
Another writer holds the SQLite write lock longer than busy_timeout during the backup API copy
```

Refusal

```text
Failed with the SQLite busy message; the copy is one write transaction, so live is unchanged
```

##### B3

```text
Postgres scratch database cannot be created or pg_restore of the credential tables fails
```

Refusal

```text
Failed with the tool's error before the live database is dropped
```

##### B4

```text
Staged backup schema has no credential tables or columns
```

Indeterminate

```text
Probe answers CannotTell; restore proceeds as the startup check does
```

### Observations

```text
DatabaseOperationStatus returned by IDatabaseManagementService.RestoreBackup or ClearDatabase: State and FailureCode
```

DatabaseOperationStatus

```text
Whether the live database changed, and which credentials were removed.
```

```text
Rows of the live database read through a Pooling=False SqliteConnection that was opened before the operation and is still open after it
```

table rows

```text
Whether the file was replaced or its pages rewritten; only content and visibility to an open connection.
```

```text
DatabaseSecretReadabilityProbe.Look over the live database with the running ring, the predicate the startup refusal uses
```

StoredSecretFinding

```text
Which values are readable beyond the first; whether non-secret data matches the backup.
```

```text
DatabaseWriteWatch of ServicePortHarness during a stopped restore: statements that wrote and whether the file changed
```

WritesSeen

```text
Writes by another process outside the harness.
```

### Equality

live database versus backup

```text
Equal when every table read through a SQL connection holds the same rows; not byte equality, since the backup API rewrites pages and header counters.
```

stopped restore

```text
Nothing changed means the live rows read before equal the rows read after and the write watch saw no write statement and no file change.
```

### Constructors

UnreadableCredentials

enum UnreadableCredentials \{ StopRestore\, Remove \}

DatabaseOperationFailure

enum DatabaseOperationFailure \{ CredentialsUnreadable \}

IStagedBackup

Task\<IStagedBackup\> IDatabaseManagementProvider\.StageBackup\(string backupContentPath\)\; IStagedBackup \: IAsyncDisposable \{ DbConnection OpenConnection\(\)\; \}

UnreadableStoredSecrets

new UnreadableStoredSecrets\(EncryptionKeyRing ring\)\.RemoveFrom\(DbConnection connection\)

### Laws

#### L1

```text
Model safety: StopChangesNothing, DoneIsStartable, DoneWithoutRemovalEqualsBackup, RemovalTakesOnlyUnreadable and StopOnlyOnNothingReadable hold in every reachable state of restore_credentials.qnt.
```

Invariant

Application

DatabaseManagementService

##### Treatment

NoTest

```text
Checked by quint verify on the model, exhaustive at the reachable depth; the runtime projection of each clause is a slice law below.
```

##### Evidence

docs\/feature\/bug\-6654\-restore\-open\-database\/models\/restore\_credentials\.qnt

### Refuted

```text
In the model, RemovalTakesOnlyUnreadable distinguishes removing only unreadable values from removing all values.
```

```text
Law scope: removal runs only after a NothingReadable finding, where every stored value is unreadable, so removing all equals removing the unreadable ones; the clause is vacuous in the model and the distinction lives in the runtime law on UnreadableStoredSecrets.
```

```text
Mutant removal filter s => false: quint run finds no RemovalTakesOnlyUnreadable violation.
```

### Claims

#### C1

```text
Credentials are removed only on an explicit UnreadableCredentials.Remove from the caller.
```

```text
Silent loss of every stored credential on an ordinary restore.
```

DatabaseManagementService

```text
Form field removeUnreadableCredentials=true
```

DatabaseManagementController\.RestoreBackup

DatabaseManagementService\.RestoreBackup

```text
Restore result Completed and the removed values
```

```text
Any in-process caller can pass Remove or cast an int to the enum; the C# compiler checks the parameter type only, not the value's origin.
```

#### C2

```text
The stop is told apart from every other failure by a closed code, not by text.
```

```text
The UI opens the remove-credentials dialog on an unrelated failure, or never.
```

DatabaseOperationStatus

```text
StoredSecretFinding NothingReadable
```

DatabaseOperationTracker\.TransitionToFailed\(id\, reason\, DatabaseOperationFailure\.CredentialsUnreadable\)

DatabaseManagementSettings

```text
failureCode CredentialsUnreadable in the JSON response
```

```text
JSON is untyped at the boundary; TypeScript narrows failureCode to a literal union, unchecked at runtime.
```

### States

Admitted

```text
Restore stopped: live unchanged, Failed with CredentialsUnreadable.
```

DatabaseManagementService

Admitted

```text
Restore completed after removal: no unreadable stored credential, connections kept with empty secrets.
```

UnreadableStoredSecrets

Admitted

```text
Restore of a partially readable or credential-free backup: live equals backup.
```

DatabaseManagementService

Excluded

```text
A completed restore whose live database has stored credentials and none readable.
```

DatabaseManagementService

Unknown

```text
Process failure after the copy and before the removal: live holds only unreadable credentials and the next start refuses with the existing start-anyway remedy.
```

maintainer

### Models

Quint

docs\/feature\/bug\-6654\-restore\-open\-database\/models\/restore\_credentials\.qnt

##### Properties

StopChangesNothing

Invariant

```text
A stopped restore leaves the live database as it was at the start.
```

DatabaseManagementService\.RestoreBackup

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:85

DoneIsStartable

Invariant

```text
A completed restore never leaves a live database whose stored credentials are all unreadable.
```

EncryptionKeyRingBootstrapper\.Resolve

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/EncryptionKeyRingBootstrapper\.cs\:125

DoneWithoutRemovalEqualsBackup

Invariant

```text
Unless the user opted in and nothing was readable, a completed restore makes the live database equal to the backup.
```

IDatabaseManagementProvider\.RestoreBackup

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IDatabaseManagementProvider\.cs\:3

RemovalTakesOnlyUnreadable

Invariant

```text
After a completed restore the live secrets are a subset of the backup's and keep every readable one.
```

UnreadableStoredSecrets\.RemoveFrom

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/UnreadableStoredSecrets\.cs\:1

StopOnlyOnNothingReadable

Invariant

```text
A restore stops only when the backup holds credentials, none readable, and the user did not opt in.
```

DatabaseManagementService\.RestoreBackup

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:85

##### Atoms

State

live

live database rows

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/DatabaseConfigurator\.cs\:1

None.

State

finding

StoredSecretFinding

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/StoredSecretReadabilityProbe\.cs\:21

None.

State

optIn

UnreadableCredentials

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/UnreadableCredentials\.cs\:1

None.

Action

check

DatabaseSecretReadabilityProbe\.Look over IStagedBackup\.OpenConnection

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:85

```text
Reads the staged copy only; no write to the live database.
```

Action

copy

IDatabaseManagementProvider\.RestoreBackup

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs\:56

```text
SQLite: one backup-API write transaction. Postgres: drop then pg_restore, not atomic.
```

Action

scrub

UnreadableStoredSecrets\.RemoveFrom

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/UnreadableStoredSecrets\.cs\:1

```text
One transaction on the live connection.
```

Action

crash

process failure

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:85

```text
Any point between check and the end of removal.
```

##### Assumptions

```text
A database is abstracted to its stored secrets, each a value id with readable or not under the running key, plus a legacy flag for a schema the probe cannot query.
```

```text
Only one restore runs at a time (DatabaseMaintenanceGate).
```

```text
Copy is atomic on the live database (one backup-API write transaction on SQLite; on Postgres pg_restore after drop is not atomic, outside this model).
```

##### Mutants

###### N1

```text
Check writes the backup to live before deciding (copy before check): quint run finds a StopChangesNothing violation.
```

###### Kills

None.

###### N2

```text
No stop (check always continues): quint run finds a DoneIsStartable violation.
```

###### Kills

None.

###### N3

```text
Removal leaves unreadable value 1 in place: quint run finds a DoneIsStartable violation.
```

###### Kills

None.

###### N4

```text
Removal runs on every opt-in, also for partially readable backups: quint run finds a DoneWithoutRemovalEqualsBackup violation.
```

###### Kills

None.

###### N5

```text
Stop also on CannotTell: quint run finds a StopOnlyOnNothingReadable violation.
```

###### Kills

None.

### Closure

```text
Restore credential check and removal
```

Safety

Pass

DesignedNotBuilt

```text
Quint verify of Safety exhaustive at depth 5 with five behavioural mutants killed (one clause vacuous, recorded as refuted).
```

##### Evidence

docs\/feature\/bug\-6654\-restore\-open\-database\/models\/restore\_credentials\.qnt

```text
Restore credential check and removal
```

Inhabitation

Pass

DesignedNotBuilt

```text
Every admitted state is built from an admitted backup and opt-in through RestoreBackup; the model reaches Stopped and Done with and without removal.
```

##### Evidence

None.

```text
Restore credential check and removal
```

PublicReachability

Pass

DesignedNotBuilt

```text
Form fields to DatabaseManagementController.RestoreBackup to DatabaseManagementService.RestoreBackup to DatabaseOperationStatus JSON and the live database; frontend dialog reads failureCode.
```

##### Evidence

None.

```text
Restore credential check and removal
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: one synchronous bounded request with no waiting on another party; every model run settles within three transitions.
```

##### Evidence

None.

```text
Restore and clear on an open SQLite file
```

Safety

Pass

DesignedNotBuilt

```text
Bounded probe on .NET 10 with Microsoft.Data.Sqlite 10.0.14: backup API into a live WAL database with an open connection and a shared file handle succeeded and the open connection read the new rows; clear from :memory: left zero tables, WAL kept.
```

##### Evidence

None.

```text
Restore and clear on an open SQLite file
```

PublicReachability

Pass

DesignedNotBuilt

```text
DatabaseManagementController to DatabaseManagementService to SqliteDatabaseManagementProvider, observed through an open connection.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Negates: no other connection writes during restore (background sync writes)
```

```text
Live database is either the old or the restored one
```

```text
Restore fails with a busy message after 10 s
```

```text
A half-copied database
```

```text
None beyond a retry by the user
```

SqliteDatabaseManagementProvider

#### Y2

Stressor

```text
Negates: backup and live database share a page size
```

```text
Live unchanged
```

```text
Restore fails: SQLite refuses to change page size of a WAL destination
```

```text
Silent partial copy
```

```text
All Lighthouse databases use the default 4096-byte page; a hand-made backup with another size cannot be restored
```

SqliteDatabaseManagementProvider

#### Y3

Stressor

```text
Negates: the process survives the restore
```

```text
Live is the old or the restored database
```

```text
Crash after copy before removal leaves only unreadable credentials
```

```text
A start that runs on unreadable credentials without saying so
```

```text
The next start refuses with the existing message naming the start-anyway setting
```

maintainer

#### Y4

Stressor

```text
Negates: the Postgres user may create databases
```

```text
Live unchanged
```

```text
Restore fails before any change
```

```text
Dropping live before the check
```

```text
Operators without CREATEDB already cannot clear or restore
```

PostgresDatabaseManagementProvider

#### Y5

SinglePointOfFailure

```text
DatabaseMaintenanceGate is per process
```

```text
One restore at a time in one process
```

```text
A second request is refused while one runs
```

```text
Two restores interleaving check and copy
```

```text
Several replicas on one Postgres database are not coordinated, as today
```

maintainer

#### Y6

Bypass

```text
Outside writer: another process or replica writes credentials between check and copy
```

```text
The check reads the staged backup, which no other process writes
```

```text
Live writes by others are overwritten by the restore, as today
```

```text
Checking live instead of the backup
```

```text
None for the staged copy
```

DatabaseManagementService

#### Y7

Stressor

```text
Second set walkthrough: double click on Restore without credentials
```

```text
One restore
```

```text
The second request is refused by the gate
```

```text
Two removals
```

```text
Walkthrough, not executed
```

DatabaseManagementSettings

#### Y8

Stressor

```text
Second set walkthrough: the key ring changes between the stop and the second request
```

```text
Check runs again with the current ring
```

```text
Now readable backup restores without removal
```

```text
Removal decided on the first request's finding
```

```text
Walkthrough, not executed
```

DatabaseManagementService

#### Y9

Stressor

```text
Second set walkthrough: a long read transaction is open during the backup-API copy
```

```text
Copy completes in WAL mode
```

```text
The reader sees the old snapshot until its transaction ends
```

```text
Reader sees a mix of old and new pages
```

```text
Walkthrough, not executed
```

SqliteDatabaseManagementProvider

### Architectural tests

#### T1

```text
The secret read path decides readability by inspecting values, never by catching: UnreadableStoredSecrets and the staged-backup types add no catch clause or exception filter on that path; the existing source-structure law covers the Encryption directory as it grows.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/SecretReadPathSourceStructureTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~SecretReadPathSourceStructureTest
```

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IStagedBackup\.cs

Create

```text
Disposable readable view of a backup before restore.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/DatabaseOperationFailure\.cs

Create

```text
Closed failure code CredentialsUnreadable.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IDatabaseManagementProvider\.cs

Extend

```text
Adds StageBackup.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/DatabaseOperationStatus\.cs

Extend

```text
Adds optional FailureCode.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseOperationTracker\.cs

Extend

```text
TransitionToFailed overload with a code.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs

Extend

```text
StageBackup over the extracted .db.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/PostgresDatabaseManagementProvider\.cs

Extend

```text
StageBackup over a scratch database with the two credential tables.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs

Extend

```text
Pre-check before the provider restore; takes IEncryptionKeyRingHolder.
```

Lighthouse\.Frontend\/src\/models\/DatabaseManagement\/DatabaseManagementTypes\.ts

Extend

```text
failureCode on IDatabaseOperationStatus.
```

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.tsx

Extend

```text
Backup key-warning Alert, Restore check line, unreadable-credentials Dialog with Cancel.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreStopsWhenNoStoredCredentialCanBeReadTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Acceptance.DatabaseManagement.RestoreStopsWhenNoStoredCredentialCanBeReadTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~PostgresDatabaseManagementProviderTest|FullyQualifiedName~DatabaseManagementServiceTest
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Settings/DatabaseManagement/DatabaseManagementSettings.test.tsx
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.test\.tsx

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/PostgresDatabaseManagementProviderTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DatabaseManagementServiceTest\.cs

#### Public oracle

```text
Store a connection with a secret option and an OAuth credential written under key ring A, create a backup through the service, replace the holder's ring with ring B that cannot read A, change a row, then call RestoreBackup with the backup and its password.
```

```text
Status Failed with FailureCode CredentialsUnreadable and the reason text that nothing has been changed; the live rows equal those before the call and the write watch saw no write and no file change. The same scenario with one secret readable under B returns Completed. Frontend: given a restore result with failureCode CredentialsUnreadable, the dialog titled Stored credentials in this backup cannot be read shows the approved body and Cancel, and Cancel closes it; the Backup section shows the key-warning Alert and the Restore section the check line.
```

```text
Today's service restores the backup (Completed) and leaves a database the next start refuses; a check placed after the provider restore changes the live rows; a dialog keyed on reason text opens on any failure.
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IDatabaseManagementProvider\.cs\:3

Changed

```text
New member StageBackup; both providers implement it in this slice.
```

wire

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/DatabaseOperationStatus\.cs\:3

Changed

```text
Trailing optional FailureCode; existing constructions compile unchanged and the JSON gains failureCode.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:85

Changed

```text
Stage, probe and stop before provider.RestoreBackup; new constructor dependency IEncryptionKeyRingHolder resolved by DI as a singleton already registered in Program.cs.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/PostgresDatabaseManagementProvider\.cs\:113

Changed

```text
StageBackup added; RestoreBackup and ClearDatabase unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DatabaseManagementServiceTest\.cs\:22

Migrated

```text
The provider mock must return a staged backup for StageBackup and the constructor takes a ring holder; the acceptance designer migrates the existing rows.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/PostgresDatabaseManagementProviderTest\.cs\:1

Migrated

```text
Gains rows for the scratch-database staging commands and the always-drop rule.
```

frontend

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.tsx\:121

Changed

```text
handleRestore branches on failureCode to open the dialog.
```

registration

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:491

UnchangedCompatible

```text
IEncryptionKeyRingHolder is already a singleton; DatabaseManagementService picks it up by constructor injection.
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IStagedBackup\.cs\:1

Changed

```text
New port type, produced by both providers.
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/DatabaseOperationFailure\.cs\:1

Changed

```text
New closed enum, serialised as a string.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseOperationTracker\.cs\:46

Changed

```text
Failed-with-code overload; existing overload unchanged.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs\:8

Changed

```text
StageBackup added.
```

wire

Lighthouse\.Frontend\/src\/models\/DatabaseManagement\/DatabaseManagementTypes\.ts\:1

Changed

```text
failureCode added to IDatabaseOperationStatus.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
New members and one new step; no existing structure is in the way.
```

##### Boundaries

```text
IDatabaseManagementService.RestoreBackup(Stream, password) built with ServicePortHarness, the real SqliteDatabaseManagementProvider and an EncryptionKeyRingHolder; frontend: DatabaseManagementSettings with a mocked IDatabaseManagementService
```

###### Driven ports

```text
IDatabaseManagementProvider.StageBackup
```

```text
DatabaseSecretReadabilityProbe over IStagedBackup.OpenConnection
```

```text
Scenario -> DatabaseManagementService -> provider StageBackup and probe; DatabaseManagementService -> DatabaseOperationTracker
```

###### Failures

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L4

```text
With StopRestore, RestoreBackup stops exactly when the staged backup's finding is NothingReadable: then State Failed, FailureCode CredentialsUnreadable; otherwise Completed with the backup's rows.
```

Algebraic

Application

DatabaseManagementService

###### Treatment

Finite

###### Cases

```text
No stored credential: Completed
```

```text
One readable and one unreadable credential: Completed
```

```text
All credentials unreadable (written under another key): Failed, CredentialsUnreadable
```

```text
All credentials readable: Completed
```

```text
Closed finite domain (readability classes times opt-in); native NUnit cases cover it, generated inputs would add no coverage.
```

###### Evidence

None.

###### L5

```text
A stopped restore changes nothing: live rows before equal live rows after, the write watch sees no write and no file change, and the gate is released.
```

Invariant

Application

DatabaseManagementService

###### Treatment

Finite

###### Cases

```text
All credentials unreadable, StopRestore
```

```text
Closed finite domain (readability classes times opt-in); native NUnit cases cover it, generated inputs would add no coverage.
```

###### Evidence

None.

###### L6

```text
Postgres StageBackup restores only the two credential tables into the scratch database and always drops it, also when pg_restore fails.
```

Invariant

Adapter

PostgresDatabaseManagementProvider

###### Treatment

Finite

###### Cases

```text
pg_restore succeeds: create, restore with both --table arguments, drop
```

```text
pg_restore fails: exception, drop still runs, live database never dropped
```

```text
Closed finite domain (readability classes times opt-in); native NUnit cases cover it, generated inputs would add no coverage.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/StoredSecretLocations\.cs

Create

```text
Single owner of the stored-secret columns, read by the probe and the removal.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/UnreadableStoredSecrets\.cs

Create

```text
Blanks unreadable secret option values and deletes OAuth credentials with an unreadable token.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/UnreadableCredentials\.cs

Create

```text
Closed opt-in StopRestore or Remove.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/StoredSecretReadabilityProbe\.cs

Extend

```text
Reads its queries from StoredSecretLocations (prefactoring).
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IDatabaseManagementService\.cs

Extend

```text
RestoreBackup takes UnreadableCredentials.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IDatabaseManagementProvider\.cs

Extend

```text
Adds CreateConnection for the live database.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs

Extend

```text
CreateConnection.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/PostgresDatabaseManagementProvider\.cs

Extend

```text
CreateConnection.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs

Extend

```text
Removal after the provider restore when opted in and nothing was readable.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs

Extend

```text
Optional form field removeUnreadableCredentials.
```

Lighthouse\.Frontend\/src\/services\/Api\/DatabaseManagementService\.ts

Extend

```text
restoreBackup sends removeUnreadableCredentials when true.
```

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.tsx

Extend

```text
Restore without credentials button in the dialog.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreWithoutUnreadableCredentialsTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Acceptance.DatabaseManagement.RestoreWithoutUnreadableCredentialsTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~DatabaseManagementControllerTest|FullyQualifiedName~DatabaseManagementServiceTest|FullyQualifiedName~StoredSecretReadabilityProbeTests|FullyQualifiedName~SecretReadPathSourceStructureTest
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Settings/DatabaseManagement/DatabaseManagementSettings.test.tsx src/services/Api/DatabaseManagementService.test.ts
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.test\.tsx

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DatabaseManagementControllerTest\.cs

Lighthouse\.Frontend\/src\/services\/Api\/DatabaseManagementService\.test\.ts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DatabaseManagementServiceTest\.cs

#### Public oracle

```text
Store a connection with a secret option, a non-secret option and an OAuth credential under key ring A, create a backup through the service, replace the holder's ring with ring B, then call RestoreBackup with the backup, its password and UnreadableCredentials.Remove. Frontend: from the open dialog click Restore without credentials.
```

```text
Status Completed; DatabaseSecretReadabilityProbe over the live database with ring B answers NothingStored, so the startup refusal does not fire; the connection and its non-secret option are present, the secret option value is empty and the OAuth credential row is gone. With one readable secret and Remove, the live database equals the backup. Controller: form field true reaches the service as Remove, absent as StopRestore. Frontend: the second call carries removeUnreadableCredentials true with the same file and password, the button shows loading, the dialog closes and the success Alert shows; a failing second call shows the error Alert.
```

```text
Without removal the probe answers NothingReadable and the next start refuses; deleting whole connections fails the connection-present check; removing on a partially readable backup fails the equals-backup check.
```

##### Promises

P3

#### Prefactoring

```text
Move the three stored-secret queries from DatabaseSecretReadabilityProbe into a new StoredSecretLocations owner that the probe reads; this makes UnreadableStoredSecrets reuse the same column set instead of copying it.
```

```text
Every probe answer (NothingStored, SomethingReadable, NothingReadable, CannotTell) and the key ids seen.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Encryption\/StoredSecretReadabilityProbeTests\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~StoredSecretReadabilityProbeTests
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IDatabaseManagementService\.cs\:3

Changed

```text
RestoreBackup gains the UnreadableCredentials parameter; the controller and test call sites move with it.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/BehaviourSettings\/Story6084HistoryFillAlwaysOnSpecifications\.cs\:160

Migrated

```text
Calls RestoreBackup; passes StopRestore if the call is on the service port, unchanged if it is on the provider.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DatabaseManagementControllerTest\.cs\:152

Migrated

```text
Moq setups and verifies name the new argument; new rows for the form field.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/StoredSecretReadabilityProbe\.cs\:96

Changed

```text
Query list moves to StoredSecretLocations, behaviour preserved.
```

frontend

Lighthouse\.Frontend\/src\/services\/Api\/DatabaseManagementService\.ts\:54

Changed

```text
Optional third argument; existing callers unchanged.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/StoredSecretLocations\.cs\:1

Changed

```text
New owner of the stored-secret columns.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Encryption\/UnreadableStoredSecrets\.cs\:1

Changed

```text
New removal owner.
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/UnreadableCredentials\.cs\:1

Changed

```text
New opt-in enum.
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/DatabaseManagement\/IDatabaseManagementProvider\.cs\:3

Changed

```text
CreateConnection added; both providers implement it in this slice.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs\:8

Changed

```text
CreateConnection added.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/PostgresDatabaseManagementProvider\.cs\:9

Changed

```text
CreateConnection added.
```

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:85

Changed

```text
Removal step after the provider restore when opted in and nothing was readable.
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DatabaseManagementController\.cs\:58

Changed

```text
Optional form field mapped to UnreadableCredentials.
```

frontend

Lighthouse\.Frontend\/src\/pages\/Settings\/DatabaseManagement\/DatabaseManagementSettings\.tsx\:121

Changed

```text
Restore without credentials button re-sends with the opt-in.
```

##### Agreement

None.

##### Not applicable

None.

##### Boundaries

```text
IDatabaseManagementService.RestoreBackup(Stream, password, UnreadableCredentials.Remove) built with ServicePortHarness and the real SqliteDatabaseManagementProvider; controller form field mapping in DatabaseManagementControllerTest; frontend DatabaseManagementSettings and DatabaseManagementService client
```

###### Driven ports

```text
IDatabaseManagementProvider.CreateConnection
```

```text
UnreadableStoredSecrets over the live connection
```

```text
Scenario -> DatabaseManagementService -> provider and UnreadableStoredSecrets -> SecretStateClassifier and StoredSecretLocations
```

###### Failures

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L7

```text
With Remove and an all-unreadable backup, RestoreBackup completes; afterwards the startup probe over the live database answers NothingStored, every connection and non-secret option of the backup is present, secret option values are empty and OAuth credentials with unreadable tokens are gone.
```

Invariant

Application

UnreadableStoredSecrets

###### Treatment

Finite

###### Cases

```text
Backup with an API-token connection and an OAuth connection, all secrets under another key
```

```text
Closed finite domain (readability classes times opt-in); native NUnit cases cover it, generated inputs would add no coverage.
```

###### Evidence

None.

###### L8

```text
With Remove and any finding other than NothingReadable nothing is removed: the live database equals the backup.
```

Algebraic

Application

DatabaseManagementService

###### Treatment

Finite

###### Cases

```text
One readable and one unreadable credential, Remove: both values kept
```

```text
No stored credential, Remove: Completed, nothing removed
```

```text
Closed finite domain (readability classes times opt-in); native NUnit cases cover it, generated inputs would add no coverage.
```

###### Evidence

None.

###### L9

```text
UnreadableStoredSecrets removes a value only when SecretStateClassifier calls it Unreadable; readable and empty values are untouched.
```

Invariant

Domain

UnreadableStoredSecrets

###### Treatment

Finite

###### Cases

```text
Readable secret kept
```

```text
Unreadable secret blanked
```

```text
Empty value untouched
```

```text
OAuth row with readable access token and unreadable refresh token deleted
```

```text
Closed finite domain (readability classes times opt-in); native NUnit cases cover it, generated inputs would add no coverage.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs

Extend

```text
RestoreBackup and ClearDatabase copy pages with SqliteConnection.BackupDatabase into a Pooling=False live connection with busy_timeout; no file replace or delete.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/DatabaseManagement\/RestoreAndClearWhileTheDatabaseIsOpenTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Acceptance.DatabaseManagement.RestoreAndClearWhileTheDatabaseIsOpenTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~SqliteDatabaseManagementProviderTest
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/SqliteDatabaseManagementProviderTest\.cs

#### Public oracle

```text
Create a backup through the service, add a row, open a Pooling=False SqliteConnection and a FileStream with FileShare.ReadWrite on the live .db (on Linux .NET then refuses an exclusive open with the same 'being used by another process' IOException Windows gives), then call RestoreBackup with the backup and its password; in a second scenario, with an open connection, add a row and call ClearDatabase.
```

```text
Restore returns Completed and the still-open connection reads the backup's rows without the added row; Clear returns Completed and the still-open connection no longer sees the added row and sees the seeded database.
```

```text
Today's file copy throws IOException 'being used by another process' on restore (status Failed); today's File.Delete leaves the open connection reading the old file, so the added row is still visible after Clear.
```

##### Promises

P1

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/SqliteDatabaseManagementProvider\.cs\:56

Changed

```text
Restore and clear mechanism replaced; CreateBackup unchanged.
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/SqliteDatabaseManagementProviderTest\.cs\:160

Migrated

```text
Rows that pin the file mechanics (replaces file, restores or removes -wal and -shm, deletes files) describe the old mechanism; the acceptance designer rewrites them to observe content, and keeps the missing-.db refusal.
```

consumer

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/DatabaseManagement\/DatabaseManagementService\.cs\:85

UnchangedCompatible

```text
Calls the same provider members; RecycleConnection, migration and seeding still follow.
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
The change is local to two methods of one provider; no preserving move makes it easier.
```

agreement

```text
Only the provider's internals change; IDatabaseManagementProvider's members and their meaning stay the same for S3.
```

##### Boundaries

```text
IDatabaseManagementService.RestoreBackup(Stream, password) and ClearDatabase(), built with ServicePortHarness and the real SqliteDatabaseManagementProvider over the harness database
```

###### Driven ports

```text
SqliteDatabaseManagementProvider over the live connection string
```

```text
Scenario -> DatabaseManagementService -> SqliteDatabaseManagementProvider -> Microsoft.Data.Sqlite
```

###### Failures

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L2

```text
After RestoreBackup completes while a connection opened before it and a FileShare.ReadWrite handle are both still open on the live file, the open connection reads exactly the backup's rows.
```

Invariant

Infrastructure

SqliteDatabaseManagementProvider

###### Treatment

Finite

###### Cases

```text
Backup whose rows are all in its .db
```

```text
Legacy backup whose newest rows are only in its -wal file
```

```text
Closed finite domain (readability classes times opt-in); native NUnit cases cover it, generated inputs would add no coverage.
```

###### Evidence

None.

###### L3

```text
After ClearDatabase completes while a connection opened before it is still open, that connection reads the empty, migrated and seeded database: rows the scenario added are gone.
```

Invariant

Infrastructure

SqliteDatabaseManagementProvider

###### Treatment

Finite

###### Cases

```text
Live database with scenario rows and an open connection
```

```text
Closed finite domain (readability classes times opt-in); native NUnit cases cover it, generated inputs would add no coverage.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

- 001 Restore and clear SQLite through the online backup API\, and check a backup\'s credentials before restoring it — Accepted
