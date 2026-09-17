# Mutation testing — Bug #6020

Both stacks are above the project's 80% minimum.

| Stack | Score | Killed | Survived | No coverage |
| --- | --- | --- | --- | --- |
| Frontend (StrykerJS 9.6.1) | **95.65%** | 88 | 4 | 0 |
| Backend (Stryker.NET 4.16.0) | **87.10%** | 54 | 4 | 2 |

Run with:

```
# frontend — the vitest config is copied into Lighthouse.Frontend/ for the run
cp ../docs/feature/bug-6020-say-what-we-know/mutation/vitest.stryker.6020.ts .
pnpm exec stryker run ../docs/feature/bug-6020-say-what-we-know/mutation/stryker.6020.frontend.json

# backend — on win-arm64 only; on x64 call dotnet-stryker directly
pwsh -NoProfile -File docs/feature/bug-6020-say-what-we-know/mutation/run-backend-x64.ps1 `
  -Config docs/feature/bug-6020-say-what-we-know/mutation/stryker.6020.backend.json
```

## What the first run found

The frontend opened at **74.79%** and the backend at **70.97%**. The gap was not noise — four
real weaknesses, each fixed by a test rather than by moving the threshold:

- **`readConnectionValidation` asserted with `toEqual`**, which ignores `undefined` properties. Every
  mutant that dropped a "only carry this key when there is a value" guard therefore survived: the
  result gained `code: undefined` and the assertion could not tell. Switched to `toStrictEqual`,
  which is what the contract actually claims. Killed four.
- **`getLogs` was never called with a tail**, so the whole query-string branch was uncovered.
- **The poller had no test for the failure mode that matters** — stopping while a request is still
  out. Added `leaves no poller behind when it stops mid-request`, plus a hidden-tab case and a
  "keeps asking" case.
- **`GetLogs(tailBytes)` had no test at the exact boundary** where the tail equals the file length,
  nor where the tail begins on the newline that ended the previous line. Both are one byte from a
  crash or a stray blank line.

It also exposed two things worth keeping:

- The existing `refreshes logs when refresh button is clicked` asserted only `toHaveBeenCalled()`,
  which was already true from the mount fetch — it would have passed against a button that did
  nothing. Now clears the mock first.
- `askAgain` carried two `stopped` guards that masked each other, so neither could be killed alone.
  Only one of them was doing anything: `clearTimeout` in the cleanup already prevents a scheduled
  re-entry, so the guard at the top of the function was dead. Removing it killed both mutants and
  removed a redundant branch.

## Scope

The frontend config mutates the changed line ranges. All four files it touches long predate this
fix, and mutating the parts it never went near would score the fix on their gaps rather than its
own. Stryker.NET does not accept the same `{start..end}` syntax alongside a `**/` glob — it silently
filters every mutant out — so the backend mutates `SerilogLogConfiguration.cs` and
`LogsController.cs` whole and narrows with `test-case-filter`, as every other backend config in this
repository does.

Mutating those two whole files drags in pre-existing untested lines. Rather than re-scope around
them, the gaps were covered: `LogPath` with and without a file sink, `GetLogs` with nothing writing
a log file, and `GetRecentProblems`, which had no test at all. That took the backend from 74.19% to
87.10%.

The Jira connector's changed lines are not mutated. The change there is one log call and one
argument inside a catch, and Stryker.NET's line-range filter does not work on that path; the
behaviour is pinned by `ValidateConnection_SomethingUnforeseenGoesWrong_SaysWhatItWasAndWritesItDown`.

## Surviving mutants

Frontend — all four equivalent:

| Location | Mutant | Why it survives |
| --- | --- | --- |
| `LogSettings.tsx:41` | `useCallback` dependency array to `[]` | `logService` never changes within a mounted page |
| `LogSettings.tsx:101` | `useEffect` dependency array to `[]` | same |
| `LogSettings.tsx:137`, `:147` | `sx={{…}}` to `{}` | spacing, not behaviour |

Backend:

| Location | Mutant | Why it survives |
| --- | --- | --- |
| `SerilogLogConfiguration.cs:47` | drop the `logFolderPath` guard | falls through to a read that throws and is caught into the same message — equivalent |
| `SerilogLogConfiguration.cs:56` | drop the "no files" guard | `.First()` on an empty array throws into the same message — equivalent |
| `SerilogLogConfiguration.cs:60` | `First()` to `FirstOrDefault()` | null then throws into the same message — equivalent |
| `SerilogLogConfiguration.cs:60` | `OrderByDescending` to `OrderBy` | **not equivalent** — it would read the oldest log file. Untestable as written: the fixture's file names do not exist on disk, so every `FileInfo.LastWriteTime` is the same value and no ordering can be asserted. Pre-existing; left alone rather than reshaped around a mutant |
| `SerilogLogConfiguration.cs:101` | `firstLineBreak < 0` branch always taken | `tail[(-1 + 1)..]` is `tail` — equivalent |
| `SerilogLogConfiguration.cs:25` | `?? ""` to another string | any unrecognised level parses to `Information` — equivalent |
| `SerilogLogConfiguration.cs:139` | `?? string.Empty` | `Path.GetDirectoryName` of a rooted path does not return null here — equivalent |
| `LogsController.cs:69` | log message text | diagnostic text, pre-existing, not behaviour |
