# Epic #5775 slice 04 (Story #5779) — mutation testing results (2026-08-16)

| stack | score on the scoped surface | killed | survived | no coverage | config |
| --- | --- | --- | --- | --- | --- |
| backend (Stryker.NET 4.16.0) | **96.15 %** (100 / 104) | 100 | 3 | 1 | `stryker.5779.backend.json` |
| frontend (StrykerJS 9.6.1) | **86.40 %** (108 / 125) | 108 | 17 | 0 | `stryker.5779.frontend.json` |

Both are above the 80 % gate. The backend opened at **75.00 %** and needed two corrections, one of
which was a configuration mistake that reads exactly like a coverage problem.

## Read this before running it

The three traps from slices 02 and 03 all still apply, and one of them cost a full fourteen-minute run
here. They are restated in those files; what follows is only what is new.

**The `test-case-filter` has to name the new fixture, or its file scores near zero and you will go
looking for the missing tests.** `PublishedKeySecretCountTests` matched none of the four
`FullyQualifiedName~` clauses inherited from slice 03, so every mutant in `PublishedKeySecretCount.cs`
survived — 7 killed of 23 — while the fixture itself was green in `dotnet test`. The report reads as
"this file is untested"; the truth was "Stryker ran none of its tests". Whenever a slice adds a test
class, add it to the filter in the same edit as the `mutate` entry.

**Do not run a frontend build while a backend run is starting up.** `pnpm build` writes into
`Lighthouse.Backend/wwwroot`, and Stryker copies the backend project into its sandbox at exactly that
moment; the run died on `Microsoft.NET.Sdk.StaticWebAssets.Compression.targets(276,5)` with several
dozen identical asset errors and no hint of the cause. One at a time.

**The `mutate` globs do apply, even though the mutant count says otherwise.** The log says
`15583 mutants created` — the whole backend — and that is alarming after slice 03's note about
mutating everything. It is a creation-phase count; the per-file report contains only the six scoped
files. Judge the scoping from the report, not from that line.

## What the gap was made of

Two of the three real holes were in the thing the slice exists to guarantee, and both are now closed
in the production code rather than papered over in a test.

**A key id that begins with another key's id.** The candidate predicate matches on
`LH1.<active key id>.` and nothing asserted that the trailing separator was there. Drop it and every
secret stored under `k-2026-08-16-11` looks as though it is already on `k-2026-08-16-1` — so a
rotation walks straight past it and reports success. Keys are named after the day they were minted
plus a counter, so this is not a contrived id: it is the eleventh key minted in one day. Now pinned by
a test that stores under the longer id while the shorter one is in force.

**A check over a large instance could not be abandoned.** `ThrowIfCancellationRequested` inside the
walk had no test, so removing it changed nothing observable. On a Tenant-Zero-sized instance that is
the difference between a request that gives up when the browser goes away and one that reads every
stored credential regardless. Now pinned.

**The published-key count had no unit tests at all.** It was reached only through the controller,
which exercised one shape of stored value and no OAuth token at all. `PublishedKeySecretCountTests`
now covers both stored-value shapes, both token columns, the empty column, the not-a-secret column,
and the two counted together — thirteen tests, and 22 of the file's 23 mutants die.

## What survives, and why each one is left

| file | line | mutant | why it is left |
| --- | --- | --- | --- |
| `SecretReadabilityReport.cs` | 48 | `ArgumentNullException.ThrowIfNull(secrets)` removed | Equivalent. The collection expression on the next line spreads `secrets`, and spreading null already raises the same exception, so the guard is a statement of intent rather than a behaviour. |
| `PublishedKeySecretCount.cs` | 19 | the `.` after the published key's id | Only reachable by a key whose id begins with `k-legacy-default`, and that id is compiled in and unique. Killing it would mean asserting against a key that cannot exist. |
| `SecretCustodyService.cs` | 110 | `ThrowIfCancellationRequested` inside the loop | Covered for a token cancelled before the walk begins; cancelling *between* two rows needs a hook into the loop that exists only for the test. The abandonment promise is kept by the covered case. |
| `SecretCustodyService.cs` | 161 | the `NotSupportedException` message for a fourth secret column | No coverage, by design. There is no fourth column; the throw exists so that adding one is a compile-and-crash rather than a silent write into the refresh token. |

The frontend's seventeen survivors are all MUI presentation: `sx={{ mt: 2 }}` emptied, a `variant`
string blanked, the `"check"` discriminator emptied (equivalent — anything that is not `"move"` reads
as a check). No behavioural mutant survives in `EncryptionService.ts` or `SecretReadabilityReport.ts`,
both of which are at 100 %.

## Reproducing

One after the other, never overlapping, and with no build running in either tree.

Backend, from `Lighthouse.Backend/Lighthouse.Backend.Tests/` — copy the config in first, because the
`mutate` globs resolve relative to the config file's own directory:

```
cp ../../docs/feature/epic-5775-secret-encryption-key-custody/mutation/stryker.5779.backend.json stryker-config.epic-5775-slice-04.json
rm -rf ../Lighthouse.Backend/data-protection-keys ../Lighthouse.Backend/keys
dotnet stryker --config-file stryker-config.epic-5775-slice-04.json --output StrykerOutput-5779
```

Frontend, from `Lighthouse.Frontend/`:

```
pnpm exec stryker run ../docs/feature/epic-5775-secret-encryption-key-custody/mutation/stryker.5779.frontend.json
```

Empty `Lighthouse.Backend/Lighthouse.Backend/data-protection-keys` and `.../keys` before and after a
backend run, per slices 02 and 03: both are gitignored, both accumulate, and a machine whose two
directories happen to agree passes a suite that fails on a clean runner.
