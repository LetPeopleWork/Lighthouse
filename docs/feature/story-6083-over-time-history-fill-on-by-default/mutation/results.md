# Mutation testing — 6083 (history fill on by default)

Run 2026-10-01 against `main` @ `4e4865f2c` (story commits, before the rebase onto `origin/main`). The gate is
80 % kill rate on each stack the story touched. The maintainer asked for the cheap run: only the two
changed backend files, one pass.

| stack | scope | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | changed lines of the two files | **93.10 %** | 29 | 27 | 2 | 0 | ~6 min |
| Backend (Stryker.NET) | whole files, as mutated | 45.60 % | 182 | 83 | 80 | 19 | ~6 min |
| Frontend | — | N/A: the story changes no frontend code | | | | | |

Config: `stryker.6083.backend.json`.

## Backend

Stryker.NET mutates whole files and ignores line ranges, so the whole-file score mixes this story's ~40
changed lines with ~400 untouched lines of backup, encryption and zip handling. The gate is read on the
changed lines (from `git diff -U0 c217cf0a8..HEAD`):

| file | changed-line mutants | killed | survived | score | whole-file score |
| --- | --- | --- | --- | --- | --- |
| `Seeding/OptionalFeatureSeeder.cs` | 23 | 21 | 2 | **91.30 %** | 70.31 % |
| `DatabaseManagement/DatabaseManagementService.cs` | 6 | 6 | 0 | **100 %** | 32.20 % |

The test filter includes `Story6083HistoryFillOnByDefaultTest`, which boots the app (11 tests). That is the
only place the restore and clear paths are exercised end to end, and the unit-only filter of the first pass
left `MigrateAndSeedDatabase(restoredFromBackup: true/false)`, the `if (restoredFromBackup)` branch and the
settle call as survivors. With the class included, all of them are killed. The rest of the acceptance suite
stays excluded, which keeps the run in minutes.

### Accepted survivors (changed lines)

- `OptionalFeatureSeeder.cs` — `GetOptionalFeatures().Single(...)` → `SingleOrDefault(...)`: equivalent. The
  list always contains the history fill's definition, so both return the same row.
- `OptionalFeatureSeeder.cs` — the record's `Value = "true"` → `""`: equivalent. Only the record's
  presence is ever read.

### Survivors outside this story's lines

Not triaged here. They are log messages, backup and restore plumbing (temp directories, manifest fields,
encryption and zip extraction), and the defaults of the other optional features. None of them changed in
this story, and the maintainer limited this pass to the cheap scoped run.
