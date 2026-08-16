# Epic #5775 slice 02 (Story #5024) — mutation testing results (2026-08-16)

| stack | score on the scoped surface | killed | survived | no coverage | config |
| --- | --- | --- | --- | --- | --- |
| backend (Stryker.NET 4.16.0) | **82.35 %** (252 / 306) | 252 | 54 | 0 | `stryker.5024.backend.json` |
| frontend (StrykerJS 9.6.1) | **91.67 %** (22 / 24) | 22 | 2 | 0 | `stryker.5024.frontend.json` |

Both are above the 80 % gate. The first run scored 84.36 % backend and 75.00 % frontend on a narrower
scope; what follows is what changed and why, because most of it was not "more tests" but tests that
were asserting the wrong thing, a config that was measuring the wrong lines, and a defect that stopped
the product booting.

## The defect this run found, and the one it did not find on its own

CI is what found it. The `Verify Backend / backend` job went red with 243 failures spread across
fixtures with nothing to do with encryption — `OAuthCredentialTest`, `DeliveryRepositoryTest`, the
Redis backplane tests — every one of them the same startup refusal.

**The old location is also the current one.** `Program.LegacyKeyStoreDirectoryName` and
`KeyStoreResolver.DefaultDirectoryName` are both `"data-protection-keys"`. That directory is what the
migration carries keys *away from*, and it is also where an instance with no database file to sit
beside still keeps its keys today. So a deployment that has booted on Postgres and on SQLite has a
real, live key store in each directory, holding different Data Protection keys and a different
`oauth-state-secret.protected`. The migration compared the two directories for equality, found they
differed, concluded there were two rival key stores and refused to start. One test assembly does both
kinds of boot, so it reproduces on a clean runner every time.

Reproduced locally by emptying both directories and running the suite: **251 failures**, and the two
directories afterwards held

```
data-protection-keys/  key-86cf1b7a….xml  oauth-state-secret.protected
keys/                  key-cb6addc8….xml  oauth-state-secret.protected
```

**The fix is a narrower definition of "rival".** Only a key ring names a key store, and two rings that
are not the same key cannot both belong to this database — that is the one case where choosing wrong
loses secrets, and the only one that now refuses. Everything else the resolved store is missing gets
carried across, which can only make more of what is already stored readable; same-named files are left
alone. Once the resolved store holds a ring of its own, the legacy directory is not consulted at all.
After the fix: **5331 passing from empty directories, and 5331 again on a second consecutive run**
against the directories the first run populated, with the legacy Data Protection key now present in
`keys/` because it was carried across.

An earlier attempt in this same session (`785de1e9c`) fixed a real but smaller version of this — the
second boot after an upgrade, where the instance's own newly minted ring made the two directories
differ. It assumed the legacy directory goes quiet once the carry-over is done. It does not, and that
attempt is superseded by the rule above.

## Per file, backend

| file | killed | survived | score |
| --- | --- | --- | --- |
| `KeyStoreResolver.cs` | 40 | 0 | 100.00 % |
| `ConfiguredKeyRingSource.cs` | 11 | 0 | 100.00 % |
| `EncryptionController.cs` | 3 | 0 | 100.00 % |
| `EncryptionKeyRing.cs` | 20 | 1 | 95.24 % |
| `MountedFileKeyRingSource.cs` | 12 | 1 | 92.31 % |
| `KeyRingSerializer.cs` | 68 | 9 | 88.31 % |
| `GeneratedKeyRingStore.cs` | 25 | 6 | 80.65 % |
| `KeyStoreMigration.cs` | 25 | 7 | 78.12 % |
| `DatabaseSecretPresenceProbe.cs` | 6 | 2 | 75.00 % |
| `StartupBanner.cs` | 31 | 21 | 59.62 % |
| `EncryptionStateDto.cs` | 0 | 1 | 0.00 % |
| `LegacyDefaultEncryptionKey.cs` | 0 | 1 | 0.00 % |

The two zeroes are files whose only mutable statement is an argument guard. `StartupBanner.cs` is
mostly relocated code — see below.

## The config was measuring less than the slice wrote

Two scope defects, both of which make a score describe code other than the one under test. Neither is
visible in the number itself, which is what makes them worth writing down.

**A whole file was missing.** `StartupBanner.cs` changed by 101 lines in this slice and was not in the
backend `mutate` list — even though the same config's `test-case-filter` already named
`FullyQualifiedName~StartupBanner`, so its tests were being run and its mutants were not. Found by
diffing `a69caf4a4..HEAD` for production files and checking each one against the list, which is the
check that should have been made when the config was written.

**The frontend line spans were four lines out of date.** `SystemSettingsTab.tsx` was scoped to
`100-111` and `214`; the code had moved to `96-107` and `209`. StrykerJS honours spans exactly as
written and mutated the wrong region.

**Rule for the next config**: derive the `mutate` list from `git diff --name-only <slice-base>..HEAD`
rather than from the roadmap, and take line spans from `grep -n` at the moment of the run.

## What the runs exposed in the tests

**A refusal nothing reached.** Naming `Encryption__KeysFile` and mounting nothing there was the one
branch in the resolution path with no test at all — five mutants, all `NoCoverage`. It is also the case
an operator is most likely to hit, because it is what a secret that failed to mount looks like.

**Assertions that stopped one step short.** Every refusal message was checked for the path it names and
nothing else, so the sentence telling an operator what to do about it could be emptied unnoticed. Those
sentences are the point of the refusal.

**Drive-letter detection exercised only inputs where every clause agreed.** `HasDriveLetterPrefix` is
four `&&`-ed clauses; every path any test supplied made all four true or all four false, so three
`&&`→`||` mutants and the `> 2`→`>= 2` boundary survived. Killing them took a database name as short as
a drive letter (`c:`), one whose third character is a separator but whose first is not a letter
(`9:/…`), and a two-character relative directory (`db/…`). That file now scores 100 %.

**Key names that never used the edge of the alphabet.** `IsUsableKeyIdCharacter` allows `a`–`z`, `0`–`9`
and `-`; no test name contained a `z` or a `9`, so shrinking either range survived. Nor did any test set
a name to nothing at all, which is what `keyId.Length is > 0` refuses.

**A frontend assertion that could not fail.** The custody-wording test read its expected text out of
`KEY_CUSTODY_WORDING` — the constant the component renders — so emptying any of the four phrasings left
`toHaveTextContent("")` matching anything. Three of the six frontend survivors were that one tautology.
The wording is now written out in the test, with a separate check that every custody the API can return
has a phrasing on screen.

**The banner never said which version it was.** The one line of the startup banner carried by no label,
and therefore the only one the existing label test could not notice going missing.

## The 54 backend survivors, by kind

**Argument guards (13).** `ArgumentNullException.ThrowIfNull` / `ThrowIfNullOrWhiteSpace` in
constructors and entry points: `EncryptionKeyRingBootstrapper` 61-65, `GeneratedKeyRingStore` 68-71,
`StartupBanner` 25/67/68, `MountedFileKeyRingSource` 18, `EncryptionStateDto` 10,
`LegacyDefaultEncryptionKey` 20, `DatabaseSecretPresenceProbe` 37, `EncryptionKeyRing` 49. Removing one
turns an `ArgumentNullException` at the call site into a `NullReferenceException` a line later.
Accepted rather than tested.

**Banner rendering (18).** Emoji glyphs and blank spacer lines in `StartupBanner.BuildInfoLines` —
`"🖥️"` → `""` and `""` → `"Stryker was here!"`. The labels themselves (`Url`, `OS`, `Runtime`,
`Architecture`, `Process ID`, `Database`, `Logs`, `Authentication`, `Authorization`) are all killed by
the existing label test, and the encryption line the slice added is killed by four tests. What survives
is cosmetic, and it is code this slice moved out of `Program.cs` rather than wrote — mutating it scores
the pre-existing suite. Deliberately not chased.

**Message decoration (5).** `KeyRingSerializer` 29, 61, 92, 106 and `GeneratedKeyRingStore` 95 — the
positional prefix and the parenthesised key name inside defect messages. The messages are asserted for
substance; how the entry is labelled is not.

**Equivalents, demonstrated rather than assumed (10).**
- `KeyStoreMigration:116` `EnumerateFiles(directory, "*")` → `""`. Applied by hand and the migration
  tests still pass: this runtime's enumerator treats an empty search pattern as match-all.
- `KeyStoreMigration:115` `Order()` → `OrderDescending()`. Both sides of every comparison are ordered
  the same way.
- `KeyStoreMigration:97` `File.Copy(overwrite: false)` → `true`. Only files the resolved store lacks are
  ever copied, so the destination never exists.
- `KeyStoreMigration:91` `Directory.CreateDirectory(resolvedDirectory)` removed. The next line creates
  the same directory as the parent of the first file copied.
- `KeyStoreMigration:26/32/42` the three early returns removed. Each falls through to code reaching the
  same outcome by a longer route — an empty legacy directory has nothing missing to carry, and a settled
  store has nothing the legacy one holds. Covered branches, equivalent mutants.
- `GeneratedKeyRingStore:152` and `DatabaseSecretPresenceProbe:59`, both catch blocks whose removal the
  runner reports as surviving on paths the tests demonstrably take.

**Arithmetic and conditionals with no observable effect (8).** `KeyRingSerializer` 38, 98 (×2), 106,
114 — a decode buffer sized larger than needed, and ternaries whose two arms agree on the inputs any
entry can present.

## The two frontend survivors

Both in `SystemSettingsTab.tsx`, both equivalent under the component as it stands:

- `104` — emptying `catch { setKeyState(null) }`. `keyState` is already `null` there and no path in the
  page refetches after a success. It stops being equivalent the moment the page gains a refresh.
- `107` — `useCallback` dependencies emptied. The closure is correct on first render and nothing flips
  `isSystemAdmin` without remounting.

## Still not measured

`Program.cs` is deliberately absent from the backend `mutate` list — 1500 unrelated lines and no
line-span option in the .NET runner. `EnsureEncryptionKeyRing`, `InitializeKeyStore` and the call that
builds the custody banner therefore carry integration tests but no mutation score. Unchanged from
slice 01, and it is why the `StartupBanner.cs` omission mattered: moving code out of `Program.cs` is the
moment it becomes measurable, and this config did not measure it until it was corrected.

## Reproducing

Run the two stacks one after the other; overlapping them puts both above the memory the frontend run
needs and returns a result that is 100 % `Timeout`.

Backend, from `Lighthouse.Backend/Lighthouse.Backend.Tests/`:

```
dotnet stryker --config-file stryker-config.epic5775-slice02.json --output StrykerOutput-5024
```

Frontend, from `Lighthouse.Frontend/` — needs a `vitest.stryker.mutation.ts` whose `include` lists
`EncryptionService.test.ts` and `SystemSettingsTab.test.tsx` only, because sweeping all 307 spec files
exhausts the node heap:

```
NODE_OPTIONS=--max-old-space-size=8192 pnpm exec stryker run stryker-config.epic5775-slice02.json
```

Before trusting a backend run, empty `Lighthouse.Backend/Lighthouse.Backend/data-protection-keys` and
`.../keys` first. Both are gitignored, both accumulate across runs, and a developer machine whose two
directories happen to agree will pass a suite that fails on a clean CI runner — which is exactly what
hid the boot defect above.
