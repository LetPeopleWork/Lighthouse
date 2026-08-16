# Epic #5775 slice 02 (Story #5024) — mutation testing results (2026-08-16)

| stack | score on the scoped surface | killed | survived | no coverage | config |
| --- | --- | --- | --- | --- | --- |
| backend (Stryker.NET 4.16.0) | **87.24 %** (212 / 243) | 212 | 31 | 0 | `stryker.5024.backend.json` |
| frontend (StrykerJS 9.6.1) | **91.67 %** (22 / 24) | 22 | 2 | 0 | `stryker.5024.frontend.json` |

Both are above the 80 % gate. The first run scored 84.36 % backend and 75.00 % frontend; what follows
is what the difference was made of, because most of it was not "more tests" but "tests that were
asserting the wrong thing".

## The run found a defect that stopped an upgraded instance booting a second time

Not a mutant — the twenty `WebApplicationFactory` tests that had been green all week failed on the
machine running this, because that machine had by then started twice.

Carrying the key store across leaves the legacy directory where it was, and the instance then writes
its own key into the resolved one. From the second start onwards the two directories differ by exactly
that file, `SequenceEqual` read that as two rival key stores, and startup refused with *"Two key stores
were found and they do not hold the same keys"*. Every instance upgrading on the default SQLite layout
would have started once and then never again.

The dogfood proofs recorded in the slice brief did not catch it because each of them booted once.

Fixed in `785de1e9c`: the invariant is that nothing in the legacy store would be lost by walking away
from it, not that the two stores are identical. A legacy key missing from the resolved store, or one
present under the same name holding different bytes, still stops startup.

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
| `KeyStoreMigration.cs` | 16 | 5 | 76.19 % |
| `DatabaseSecretPresenceProbe.cs` | 6 | 2 | 75.00 % |
| `EncryptionStateDto.cs` | 0 | 1 | 0.00 % |
| `LegacyDefaultEncryptionKey.cs` | 0 | 1 | 0.00 % |

The two zeroes are files whose only mutable statement is an argument guard; see below.

## What the first run exposed, and what was done about it

**A refusal nothing reached.** Naming `Encryption__KeysFile` and mounting nothing there was the one
branch in the whole resolution path with no test at all — five mutants, all `NoCoverage`. It is also
the case an operator is most likely to hit, because it is what a secret that failed to mount looks
like.

**Assertions that stopped one step short.** Every refusal message was checked for the path it names
and for nothing else, so the sentence telling an operator what to do about it could be emptied without
a test noticing. Those sentences are the whole point of the refusal.

**Drive-letter detection exercised only inputs where every clause agreed.** `HasDriveLetterPrefix` is
four `&&`-ed clauses; every path any test supplied made all four true or all four false, so three
`&&`→`||` mutants and the `> 2`→`>= 2` boundary all survived. Killing them took a database name as
short as a drive letter (`c:`), one whose third character is a separator but whose first is not a
letter (`9:/…`), and a two-character relative directory (`db/…`). That file now scores 100 %.

**Key names that never used the edge of the alphabet.** `IsUsableKeyIdCharacter` allows `a`–`z`,
`0`–`9` and `-`; no test name contained a `z` or a `9`, so shrinking either range survived. Nor did any
test set a name to nothing at all, which is what `keyId.Length is > 0` is there to refuse.

**A frontend assertion that could not fail.** The custody-wording test read the expected text out of
`KEY_CUSTODY_WORDING` — the same constant the component renders — so emptying any of the four
phrasings left `toHaveTextContent("")` matching anything. Three of the six frontend survivors were
that one tautology. The wording is now written out in the test, with a separate check that every
custody the API can return has a phrasing on screen.

**Frontend line spans four lines out of date.** `stryker.5024.frontend.json` scoped `SystemSettingsTab.tsx`
to `100-111` and `214`; the code had moved to `96-107` and `209`. StrykerJS honoured the spans exactly
as written and mutated the wrong region — the failure mode is a score that describes code the slice
never touched. Take spans from `grep -n` at the moment of the run, never from the roadmap.

## The 31 backend survivors, by kind

**Argument guards (13).** `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`
in constructors: `EncryptionKeyRingBootstrapper` 61-65, `GeneratedKeyRingStore` 68-71,
`MountedFileKeyRingSource` 18, `EncryptionStateDto` 10, `LegacyDefaultEncryptionKey` 20,
`DatabaseSecretPresenceProbe` 37, `EncryptionKeyRing` 49. Removing one turns an `ArgumentNullException`
at the call site into a `NullReferenceException` one line later. Accepted rather than tested: a test per
guard buys a different exception type on a path no caller takes.

**Message decoration (5).** `KeyRingSerializer` 29, 61, 92, 106 and `GeneratedKeyRingStore` 95 — the
positional prefix and the parenthesised key name inside defect messages. The messages are asserted for
the substance; how the entry is labelled is not.

**Equivalents, demonstrated rather than assumed (5).**
- `KeyStoreMigration:91` `EnumerateFiles(directory, "*")` → `""`. Applied by hand and the migration
  tests still pass: this runtime's enumerator treats an empty search pattern as match-all.
- `KeyStoreMigration:90` `Order()` → `OrderDescending()`. Both sides of every comparison are ordered
  the same way.
- `KeyStoreMigration:55` `File.Copy(overwrite: false)` → `true`. `CopyAcross` only runs when the
  destination is empty.
- `KeyStoreMigration:49` `Directory.CreateDirectory(resolvedDirectory)` removed. Line 54 creates the
  same directory as the parent of the first file copied.
- `KeyStoreMigration:19` the same-directory short circuit removed. Falling through reaches
  `ResolvedHoldsEveryLegacyKey(dir, contents, dir, contents)`, which is true, and returns the same
  outcome. It was `NoCoverage` before this slice and is now covered — the mutant is equivalent, the
  branch is not untested.

**Arithmetic and conditionals with no observable effect (8).** `KeyRingSerializer` 38, 98 (×2), 106,
114 — a decode buffer sized larger than needed, and ternaries whose two arms agree on the inputs any
entry can present.

## The two frontend survivors

Both in `SystemSettingsTab.tsx`, both judged equivalent under the component as it stands:

- `104` — emptying `catch { setKeyState(null) }`. `keyState` is already `null` at that point and no
  path in the page refetches after a success, so nothing observable changes. It would stop being
  equivalent the moment the page gains a refresh.
- `107` — `useCallback` dependencies emptied. The closure is correct on first render and nothing
  flips `isSystemAdmin` without remounting.

## Still not measured

`Program.cs` is deliberately absent from the backend `mutate` list — 1500 unrelated lines and no
line-span option in the .NET runner. `EnsureEncryptionKeyRing`, `InitializeKeyStore` and the custody
banner therefore carry integration tests but no mutation score. Unchanged from slice 01.

## Reproducing

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

Run the two stacks one after the other. Overlapping them puts both above the memory the frontend run
needs and returns a result that is 100 % `Timeout`.
