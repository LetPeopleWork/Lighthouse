# Bug 6654 — Restore and Clear Database on an open SQLite file

## Maintainer decisions (2026-11-17, start of the bugfix)

- **Rigor profile:** `thorough` for every slice — crafter, acceptance designer and architect on Opus,
  reviewers and the examiner on Sonnet.
- **Scope:** the key-warning request in the bug is part of this bug, not a separate item. The maintainer
  chose "warn, check before restoring, and let the user restore without the unreadable credentials" over
  "warning text only" and over "start anyway".
- **Slices and sketch approved** (nwdes ids; delivery order S3 → S1 → S2):

  | nwdes slice | Delivers |
  |---|---|
  | S3 | Restore Backup and Clear Database work on SQLite while the app holds the file open (Windows, macOS) |
  | S1 | Backup shows the key warning, Restore says it checks first; a restore whose credentials this key cannot read stops with a clear message and changes nothing |
  | S2 | From that message, "Restore without credentials": the restore completes, unreadable credentials are removed, the app starts |

- **UI sketch (approved), Settings → Database Management, reusing `Alert` and the Clear-confirmation `Dialog`:**

  ```
   Backup
   Create an encrypted backup of the database ...            (unchanged)
   ┌ ⓘ ──────────────────────────────────────────────────────────────┐
   │ The backup does not contain this installation's encryption key.  │
   │ Its stored credentials can only be read by an installation that  │
   │ uses the same key.                                               │
   └──────────────────────────────────────────────────────────────────┘
   [Create Backup]

   Restore
   Restore the database from a previously created backup file. The
   application will restart after restore.                    (unchanged)
   Before it changes anything, Lighthouse checks that this installation's
   encryption key can read the backup's stored credentials.        (new)
   [Choose Backup File] [Password____] [Restore Backup]

   ── when the check finds no readable credential ───────────────────────
   ┌ Stored credentials in this backup cannot be read ─────────────────┐
   │ None of the stored credentials in this backup can be read with   │
   │ this installation's encryption key. Nothing has been changed.    │
   │                                                                  │
   │ If you restore anyway, Lighthouse removes these credentials so   │
   │ it can start. You must then enter each token again in its        │
   │ Connection.                                                      │
   │                          [Cancel]  [Restore without credentials] │  ← S2 adds this button
   └──────────────────────────────────────────────────────────────────┘
  ```

- **Loading and failure looks:** the Restore button keeps its existing `loading` state while the check and
  restore run; any other failure keeps the existing error `Alert`. The dialog opens only on the closed
  failure code `CredentialsUnreadable`, never on reason text.
- **The check stops a restore only** when the backup holds credentials and none can be read (the case that
  refuses startup today). Partly readable backups restore as before.
- **Usage data:** N/A — a bug fix and a safety net, not a feature whose use we want to count.
- **Context map / UI surface:** System → Database Management; touches the platform (database
  management) and Encryption contexts, no new domain events, no map or glossary change.
- **Out of scope:** letting EF own the SQLite connection (`DatabaseConfigurator`), and the backup
  creation path (`CreateBackup`).
