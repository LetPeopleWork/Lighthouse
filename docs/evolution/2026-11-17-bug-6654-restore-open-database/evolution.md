# Bug 6654 — Restore and Clear Database on an open SQLite file, and backups from another installation

**Finished:** 2026-11-17 · ADO Bug #6654 · nwdes feature `docs/feature/bug-6654-restore-open-database/`

## The problem

On Windows and macOS with SQLite, **Restore Backup** and **Clear Database** always failed with "The process
cannot access the file … because it is being used by another process". The provider replaced or deleted the live
database file, while the running application, its background services and the request itself held SQLite handles
on it. On Linux the same copy "succeeded" while open connections kept reading stale pages.

A second problem came with the report: a backup restored onto another installation left Lighthouse unable to start,
because that installation's encryption key could read none of the stored credentials, and nothing warned about it.

## What changed

Three slices, delivered in the order S3 → S1 → S2.

- **S3 — restore and clear while the file is open.** Restore and clear copy pages into the open database with
  SQLite's online backup API (`SqliteConnection.BackupDatabase`), from the staged backup file or from an empty
  in-memory database. No file is replaced or deleted; every open connection reads the new content; WAL stays on.
- **S1 — check before restoring.** The restore stages the backup (the extracted file on SQLite; a scratch database
  holding only the credential tables on Postgres, always dropped afterwards) and asks the same readability probe the
  startup refusal uses. When the backup holds credentials and this key reads none, the restore stops before
  anything changes with the closed failure code `CredentialsUnreadable`. The settings page says a backup does not
  carry the encryption key, says the restore checks first, and explains a stopped restore in a dialog.
- **S2 — restore without credentials.** From that dialog the operator can restore anyway: unreadable secret option
  values are blanked and OAuth credentials with an unreadable token are deleted, after the copy and before
  migrations, so the next start does not refuse. Connections and plain options are kept. The secret columns now
  have one owner (`StoredSecretLocations`), shared by the startup check and the removal.

ADR-286 records the decision.

## Key commits

| Commit | What |
|---|---|
| `96736d4c6`, `a16c35880` | S3: oracle, then restore/clear through the online backup API |
| `300bddf43`, `507fd21a6` | S1: oracle, then the staged credential check, Postgres scratch staging, key warning and dialog |
| `1644de3aa`, `d8f6bea97`, `f29029a7e` | S2: oracle, a fixture fix, then restore without credentials |
| `6912a1d9d`, `6d5d14e95` | Final refactor of the oracles (typed calls, missing rows, failure code pinned as text) |
| `31198099b` | Mutation survivors killed |
| `960973b03` | ADR-286 |

## Gates

- RED observed before each slice; GREEN by the host after each slice.
- Reviews: S3 and S1 embedded slice reviews accepted; S2 covered by the whole-candidate review (accepted, one low
  residual: deleting the global `JsonStringEnumConverter` in `Program.cs` would pass every test, only a host test
  could catch it).
- EXAMINE: E3, E4, E6, E7 met. E1, E2 (Windows/macOS) and E8 (re-entering a token) observed on Linux and in tests
  only; the maintainer accepted that as enough. E5 (the docs say a backup has no key) fixed in this finalize.
- Mutation: backend 86.84 % (87.17 % after the blocked-gate test), frontend 91.18 % — `mutation/results.md`.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | **Done** — `docs/settings/databasemanagement.md`: the backup has no encryption key, the restore checks first, Restore without credentials, SQLite works while running on every OS. |
| Screenshots | **Deferred, follow-up in the same session** — `Screenshots.spec.ts` "database management settings page" shows the Backup section that now carries the key notice; re-run with `--grep` on that one test after the push. |
| Demo data | **N/A**, because the feature adds no data to show; it acts on the whole database. |
| Website assets | **N/A** until the screenshot is regenerated; `settings/databasemanagement.png` is the only asset concerned. |
| Clients (CLI, MCP) | **N/A**, because Lighthouse-Clients has no database backup or restore command; the restore route only gained an optional form field. |
| Context map, `ARCHITECTURE.md` | **N/A**, because no context, edge or glossary term changed: it stays in the platform's database management with Encryption's existing probe. |
| Usage-data event | **N/A**, because a bug fix and a safety net are not something whose use we count. |
| Mutation | **Done** — see Gates. |
| Release notes | N/A; written at /release. |

## Left open, knowingly

- The Postgres staging and removal paths are covered by command-recording unit tests only; no real `pg_restore`.
- If the credential removal itself throws after the copy, the restored database stays in place with status Failed.
- The 10 s SQLite busy timeout is a literal in two places (`DatabaseConfigurator` and the provider).
- `docs/product/architecture/ADR-001.md` duplicates ADR-286 because nwdes resolves the feature's ADR by that path.
