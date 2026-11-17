# Mutation testing — 6654 (Restore refuses a backup whose credentials cannot be read)

The gate is an 80 % kill rate per stack.

## Backend

Run 2026-11-17, Stryker.NET 5.0.0, against the worktree at `9cabf6350` plus the kill-round tests.
**Score: 86.84 %** (264 killed, 33 survived, 7 no coverage; 297 mutants tested, 236 tests, 7 min 17 s). The gate passes.

Run from `Lighthouse.Backend/Lighthouse.Backend.Tests`:
`dotnet stryker --config-file ../../docs/feature/bug-6654-restore-open-database/mutation/stryker.6654.backend.json`

Scope was checked in `reports/mutation-report.json`: every tested mutant belongs to one of the eight files below.
`ignore-methods: ["*Log*", "Console.*"]` (as in the 5743 and 6306 configs) keeps log text out of the score. The first run
had no such filter and scored 74.07 % with 344 mutants tested. 47 of its 84 survivors were log statements and log text.

**Test selection.** The unit classes for these files (`DatabaseManagementControllerTest`, `DatabaseManagementServiceTest`,
`DatabaseManagementCompatibilityTest`, `DatabaseOperationTrackerTest`, `SqliteDatabaseManagementProviderTest`,
`PostgresDatabaseManagementProviderTest`, `StoredSecretReadabilityProbeTests`, `EncryptionKeyRingBootstrapperTests`)
**plus `Tests.Acceptance.DatabaseManagement`**. The acceptance scenarios run at the service port through
`ServicePortHarness` on a SQLite file, with no web host, so they take seconds. They are the only tests that cover
restore, clear and the removal of unreadable credentials from end to end. A run without them would not measure this
bug's fix. `API.Integration` and `Integration.Containers` are excluded.

Interfaces, enums and records (`IDatabaseManagementProvider`, `IStagedBackup`, `UnreadableCredentials`,
`DatabaseOperationFailure`, `DatabaseOperationStatus`) were not mutated: they hold no logic worth mutating.

| File | Killed | Survived | No coverage | Score |
| --- | --- | --- | --- | --- |
| `API/DatabaseManagementController.cs` | 27 | 0 | 0 | 100.00 % |
| `DatabaseManagement/DatabaseManagementService.cs` | 91 | 7 | 1 | 91.92 % |
| `DatabaseManagement/DatabaseOperationTracker.cs` | 11 | 0 | 0 | 100.00 % |
| `DatabaseManagement/PostgresDatabaseManagementProvider.cs` | 53 | 6 | 6 | 81.54 % |
| `DatabaseManagement/SqliteDatabaseManagementProvider.cs` | 29 | 9 | 0 | 76.32 % |
| `Encryption/StoredSecretLocations.cs` | 13 | 8 | 0 | 61.90 % |
| `Encryption/StoredSecretReadabilityProbe.cs` | 36 | 1 | 0 | 97.30 % |
| `Encryption/UnreadableStoredSecrets.cs` | 4 | 2 | 0 | 66.67 % |

### Closed by the kill round (`PostgresDatabaseManagementProviderTest`)

- **`&&` to `||` in `IsToolingAvailable` (line 48).** `IsToolingAvailable_PgDumpMissing_ReturnsFalse` and
  `IsToolingAvailable_PgRestoreMissing_ReturnsFalse` left `psql` missing too, which hid the mutant. They now say `psql`
  is present, so each test is missing only the tool it names.
- **Early return of `GetServerVersion` removed (line 74).** `GetServerVersion_ReturnsNull_WhenPgDumpNotAvailable` now
  also checks that no command runs.
- **`ExitCode == 0 ? … : null` to `true ? … : null` (line 86).** New test
  `GetServerVersion_ReturnsNull_WhenPgDumpReportsAFailure`: a non-zero exit gives no version.

### Closed after the Stryker run (`DatabaseManagementServiceTest`)

- **`DatabaseManagementService.cs:102`, the "gate not acquired" block of `RestoreBackup` removed.** No test sent a
  restore into a blocked gate, although the same block in backup (49) and clear (201) was killed. New test
  `RestoreBackup_AnotherOperationHoldsTheGate_ReturnsFailedWithBlockedReasonAndLeavesTheDatabaseAlone`: while a backup
  holds the gate, a restore returns `Failed` with the gate's blocked reason, the gate stays held, and the provider is
  never asked to stage or restore. Proven by hand rather than by a new Stryker run: with the block's `return` replaced
  (the file then restored exactly), the test fails with `Expected: "A database Backup operation is currently active."
  But was: "Invalid backup file: too short to contain encryption header."`. With every survivor but this one unchanged,
  the backend count becomes 265 killed and 32 survived, **87.17 %**.

### Accepted survivors

| File:line | Mutant | Reason |
| --- | --- | --- |
| Service:191 | `Directory.CreateDirectory(extractDir)` removed | Equivalent: `ExtractToDirectorySafely` creates each entry's directory. |
| Service:266 | `ToString("N")` to `ToString("")` | Equivalent: the id stays unique, only its spelling changes. |
| Service:289 (no coverage) | `"unknown"` version fallback | Unreachable: the assembly always has a version. |
| Service:354, 356 | delete of the decrypted temp file | Equivalent: the file is in the temp directory that `CleanupDirectory` deletes. |
| Service:371 | `aes.GenerateIV()` removed | Equivalent: `Aes.Create()` already makes a random IV. |
| Service:420 | `overwrite: true` to `false` | Equivalent: extraction goes into a new, empty directory. |
| Postgres:34, 36 (+ no coverage 34, 36, 37, 38) | defaults for a connection string without host, database, user or password | Unreached defaults: every test connection string names them all. |
| Postgres:89 (no coverage) | catch block of `GetServerVersion` | Needs a command runner that throws. Low value. |
| Postgres:131 | `Pooling = false` on the scratch database | Not observable without a server; the drop uses `WITH (FORCE)`. |
| Postgres:169, 213 | exception message text | Message text. |
| Postgres:206 (no coverage) | `NpgsqlConnection.ClearAllPools()` removed | Needs a server; the container tests are excluded. |
| SQLite:42 | missing-file throw or its message | Equivalent: the copy that follows throws `FileNotFoundException` too. |
| SQLite:81 | `Data Source=:memory:` to `""` | Equivalent: an empty data source is a private temporary database, also empty. |
| SQLite:92 | `SqliteConnection.ClearAllPools()` removed | Not observable: the provider's own connections are unpooled. |
| SQLite:110 | `PRAGMA busy_timeout` not run | Needs a concurrent writer to observe. |
| SQLite:117 | `"*.db"` to `""` | .NET reads an empty pattern as `*`, and the database is the first file found in these runs. Depends on file order. |
| SQLite:121, 146 | `Pooling = false` | Not observable on Linux, where a pooled handle does not stop the temp directory from being deleted. It matters on Windows. |
| StoredSecretLocations:16, 21, 24 | SQL text of a location | Stryker limit, not a test gap: this code runs only in the static initialiser of `StoredSecretLocations.All`, once per test process, before Stryker switches a mutant on. An empty query reads no secrets, and `RestoreStopsWhenNoStoredCredentialCanBeReadTest` would then fail. |
| StoredSecretLocations:28, 35, 36, 37; UnreadableStoredSecrets:15, 22; Probe:105 | `ArgumentNullException.ThrowIfNull` removed | Guards no caller reaches: every caller passes a non-null value under nullable annotations. |

## Frontend

Run 2026-11-17 against the worktree at `9cabf6350` plus the kill-round tests. StrykerJS, `coverageAnalysis: perTest`,
`disableTypeChecks: false`, in place.

| run | file | score | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| first | `DatabaseManagementSettings.tsx` | 82.14 % | 23 | 4 | 1 | |
| first | `DatabaseManagementService.ts` | 100.00 % | 6 | 0 | 0 | |
| first | all | 85.29 % | 29 | 4 | 1 | 37 s |
| after the kill round | `DatabaseManagementSettings.tsx` | 89.29 % | 25 | 3 | 0 | |
| after the kill round | `DatabaseManagementService.ts` | 100.00 % | 6 | 0 | 0 | |
| after the kill round | all | **91.18 %** | 31 | 3 | 0 | 14 s (incremental) |

Scope: only the lines this feature changed (`git diff d8dc2f2db HEAD`): `DatabaseManagementSettings.tsx` 47-48, 57-58,
125-131, 139-153, 171-173, 310-314, 357-361, 476-513, and `DatabaseManagementService.ts` 14, 58, 64-66.
`DatabaseManagementTypes.ts` changed too, but it holds types only and has nothing to mutate.

Configs: `stryker.6654.frontend.json` and `vitest.stryker.mutation.ts`. Copy the Vitest config into
`Lighthouse.Frontend/` for the run and remove it after it. Run from `Lighthouse.Frontend/`:

```bash
pnpm exec stryker run ../docs/feature/bug-6654-restore-open-database/mutation/stryker.6654.frontend.json --incremental
```

### Closed by the kill round

- **`onClose={() => setUnreadableCredentialsReason(null)}` emptied (survived).** No test closed the credentials
  dialog other than with Cancel. A new test closes it with Escape.
- **`result.failureReason ?? ""` to `?? "Stryker was here!"` (no coverage).** No test gave a refusal without a
  reason. A new test does, and checks that the dialog still opens and its description is empty.

### Accepted survivors

- **`sx={{ mb: 2 }}` to `sx={{}}` (3 mutants: lines 310, 357, 491).** Spacing below the encryption-key notice, the
  pre-restore check note and the dialog's reason. It changes only the margin, which jsdom does not lay out, and no
  behaviour or text depends on it.
