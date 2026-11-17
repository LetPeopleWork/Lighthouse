# Acceptance brief: bug\-6654\-restore\-open\-database

## Slices

None.

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
