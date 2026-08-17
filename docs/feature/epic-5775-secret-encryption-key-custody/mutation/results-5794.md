# Mutation testing — 5794 (A refusal that cannot quote the key)

Run 2026-08-17 against `main` @ `e14ec4c54` plus the slice 07 working tree. Gate is 80 % kill rate on
every stack with changed files.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **86.17 %** | 92 | 81 | 11 | 2 | 0 | 5 m 59 s |
| Frontend | **N/A** | — | — | — | — | — | — |

**Frontend is N/A, not skipped.** This slice changes two backend files and three backend test files.
Nothing under `Lighthouse.Frontend/` is touched, so there is no frontend stack to run.

Config: `stryker.5794.backend.json`.

## Backend

| file | killed | survived | no coverage |
| --- | --- | --- | --- |
| `KeyRingSerializer.cs` | 73 | 9 | 0 |
| `KeyStoreFile.cs` | 8 | 2 | 2 |

Three runs, not one. The first scored 86.02 % and the survivor list is what produced the two changes
below; the second was abandoned part-way when the `chmod` failure mode was found; the third is the run
reported here.

### Closed by this pass

- **`KeyRingSerializer.cs:147` — `keyId.Length <= MaxKeyIdLength` mutated to `<`.** The boundary of
  the whole slice: at exactly the longest name a key may have, does the refusal still quote it? No test
  stood on that boundary — the too-long case used 33 characters and the longest-allowed case used a
  name that is valid and therefore never reaches the branch. Now pinned by
  `Parse_ANameAsLongAsANameMayBe_IsStillQuotedWhenItsCharactersAreWrong`, which supplies 31 lowercase
  letters and one uppercase: as long as a name may be, and wrong in exactly one character.

### Accepted survivors

**`KeyStoreFile.cs:35` and `:36` — the mode asked for at creation.** Removing
`options.UnixCreateMode` leaves the resting mode correct anyway, because `CloseItToEverybodyElse`
applies it after the write. The two are not redundant — the one at creation is what stops the file
being briefly readable while the contents are still going in — but that window is not observable from
a test, only the resting state is. Killing them would mean dropping the post-write call and relying on
creation alone, which would give up healing an upgraded key store. Both layers are worth keeping and
one of them cannot be pinned.

**`KeyRingSerializer.cs:112` ×2 — the parenthetical name on an unnamed entry.** `var named = keyId is
null ? string.Empty : $" ('{keyId}')"`. Both mutants change what an entry with *no* name is described
as, and no test asserts the full sentence for a colon-less entry. Cosmetic, and pre-existing: this
slice did not touch the line.

**`KeyRingSerializer.cs:30, :39, :62, :98, :104 ×2, :120` — seven pre-existing survivors** in
`TryParse`, `Format` and the entry splitting. All on lines this slice did not change. They are the
file's existing coverage shape rather than anything this slice introduced, and closing them is
`KeyRingSerializer`'s own debt rather than this story's.

### No coverage

**`KeyStoreFile.cs` — the two mutants inside the `catch`.** The best-effort branch that lets the write
succeed when the filesystem refuses `chmod`. Nothing reaches it, because no filesystem a test can
reach declines a `chmod` on demand — the same problem `IKeyStoreFileSystem` exists to solve for the
lying-substrate case, one level down. Making it reachable would mean putting a seam under
`File.SetUnixFileMode`, which is a lot of structure for a branch whose whole body is "carry on".

Recorded rather than closed, and it is the honest reading of the number: two of the fourteen mutants
in that file are in a path this slice cannot demonstrate. What the branch protects against is real —
a key store on a volume shared from a Windows host, on exFAT, or on some network mounts, where asking
for a mode refuses and an insisted-on mode would turn a working instance into one that will not start.

### Not mutated

- **`GeneratedKeyRingStore.cs`** — the change is one line, `File.WriteAllBytes` becoming
  `KeyStoreFile.Write`. Mutating it would put 230 lines of shipped code from slice 02 into the
  denominator to judge a delegation. What that line does is covered by `KeyStoreFileTests` directly.
- **`Program.cs`** — the same one-line change, in a file of some 1900 lines. Same reasoning, more so.
