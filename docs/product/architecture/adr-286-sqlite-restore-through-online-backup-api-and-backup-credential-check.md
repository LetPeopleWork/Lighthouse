# ADR-286: Restore and clear SQLite through the online backup API, and check a backup's credentials before restoring it
Status: Accepted

## Context

```text
On Windows and macOS, Restore Backup and Clear Database failed on SQLite because they replaced or deleted the database file while the running application held it open. A backup from another installation could also be restored although this installation's key reads none of its credentials, after which Lighthouse refused to start.
```

## Decision

```text
SQLite restore and clear copy pages into the open database with SqliteConnection.BackupDatabase (from the backup file, or from an empty in-memory database to clear); no database file is replaced or deleted. Before any restore changes data, the backup is staged (the extracted file on SQLite, a scratch database with only the credential tables on Postgres) and checked with the same readability probe the startup refusal uses. When the backup holds credentials and none can be read, the restore stops with the closed failure code CredentialsUnreadable and nothing changed. With an explicit opt-in the restore completes and the unreadable secret values are blanked and unreadable OAuth credentials deleted, so the next start does not refuse and the user enters each token again.
```

## Alternatives

```text
Close every SQLite handle (let EF own connections, clear pools, wait for finalisers) and keep the file copy: cannot reach handles held by background work or the request itself, and still breaks on Windows. Restore to a side file and swap it on restart: needs a restart protocol and leaves a window with two databases. Check credentials after the restore: breaks the promise that a stopped restore changes nothing. Parse pg_restore output on Postgres: brittle COPY escaping. Delete whole connections instead of their secrets: the user would have to rebuild every connection, not only re-enter tokens.
```

## Consequences

```text
Restore and clear work with open connections on every OS and journal_mode stays WAL. A restore needs a write lock for one transaction, so a long writer makes it fail after the busy timeout instead of corrupting data. Postgres restore creates and drops a scratch database and needs CREATEDB, which clear already needs. A crash between copy and credential removal leaves a database the next start refuses, with the existing start-anyway remedy.
```
