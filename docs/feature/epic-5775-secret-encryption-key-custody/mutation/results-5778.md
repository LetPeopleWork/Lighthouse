# Epic #5775 slice 03 (Story #5778) — mutation testing results (2026-08-16)

| stack | score on the scoped surface | killed | survived | no coverage | config |
| --- | --- | --- | --- | --- | --- |
| backend (Stryker.NET 4.16.0) | **84.66 %** (138 / 163) | 137 + 1 timeout | 19 | 6 | `stryker.5778.backend.json` |
| frontend (StrykerJS 9.6.1) | **87.25 %** (89 / 102) | 89 | 13 | 0 | `stryker.5778.frontend.json` |

Both are above the 80 % gate. The backend opened at **67.48 %** and the frontend at **56.19 %**; what
follows is what the gap was made of, because one part of it was a real hole in the thing this slice
exists to guarantee.

## Read this before running it

Two of the three failed launches in this session were already written down, and reading them first would
have saved about twenty minutes.

**Run the backend from `Lighthouse.Backend/Lighthouse.Backend.Tests/`.** That is the one working
directory where all three paths in the config resolve: `../Lighthouse.sln` finds the solution and
`Lighthouse.Backend.Tests.csproj` is local. Run it from the solution directory instead and Stryker
silently ignores the `mutate` globs and mutates the entire backend — 15 536 mutants, ten minutes, and a
number that describes the whole product rather than this slice. It does not warn; the only symptom is
the mutant count. Slice 02's results file says this under *Reproducing*.

**Every file named in a mutation config needs its covering spec in `vitest.stryker.mutation.ts`.** That
file deliberately narrows the spec list, because sweeping all 308 specs exhausts the node heap. It is
not a performance tweak you can ignore: a mutated file whose spec is missing from that list has *no test
running against it at all*, so every one of its mutants survives and the report reads as a coverage
problem rather than a configuration one. `EncryptionPanel.test.tsx` was added to it for this slice.

**A frontend run makes the working tree temporarily unbuildable.** The config sets `inPlace: true`, so
Stryker rewrites the real `src/` files mutant by mutant and restores them from backup at the end. For
the ~100 seconds of a run, `pnpm build` and any IDE type-check fail on deliberately corrupted source.
Tell whoever else is in the repo before starting one. The backend runs are safe — they work in a
sandbox copy.

Empty `Lighthouse.Backend/Lighthouse.Backend/data-protection-keys` and `.../keys` before trusting a
backend run, per slice 02: both are gitignored, both accumulate, and a machine whose two directories
happen to agree passes a suite that fails on a clean runner.

**And empty them again afterwards** — this half is new, and it cost twenty minutes here. A mutation run
boots the application thousands of times with the key-resolution code mutated, and those boots mint key
rings. They do not all land in the same directory, because which store a boot resolves to is one of the
things being mutated. Both directories end up holding a different `encryption-keyring.protected`, and
the next ordinary `dotnet test` then fails wholesale — every `WebApplicationFactory` fixture in the
suite, most of them nothing to do with encryption — with

```
Two key rings were found and they are not the same key
```

That is slice 02's guard working correctly: two stores hold different keys and the product will not
guess which belongs to this database. It is a developer-machine artefact, not a product defect — a real
deployment has one boot path — and CI never sees it because every runner starts clean. Delete the
stray `encryption-keyring.protected` and the suite is green again.

## The hole this run found

**The compare-and-swap was untested on the two columns that matter most.**
`SecretCustodyService.cs:146` and `:150` mutate `&&` to `||` in the guarded update for
`OAuthCredential.AccessToken` and `.RefreshToken`. Both survived.

With `||`, the statement matches on row id *or* observed value, so it writes over a token regardless of
whether the value is still the one it read. That is exactly the credential destruction the whole slice
is built to prevent, and the mutant sailed through 5 407 green tests.

The cause is narrow and worth naming: the concurrency test drove its interference through a
*connection option*. That kills the equivalent mutant on line 142 and leaves the two OAuth columns
covered by nothing. The columns that a token refresh actually rewrites — the ones ADR-151's whole
argument is about — were the ones with no guard on their guard.

Four tests now cover it: an access token and a refresh token each rewritten between the read and the
write, and the two cases where only *one* of a credential's two tokens is stale. The last pair matters
on its own: the candidate query fetches a row when either token needs moving, and nothing was checking
that the other one did not get stranded on a retired key with nothing ever coming back for it.

## What else the gap was made of

**Backend, 67.48 % → 84.66 %.** Beyond the four above: `StillNeedsMoving` had no empty-token case; every
constructor's boundary refusal was unasserted; the per-custody refusal messages were blank-able, which
matters because the sentence is what tells an administrator *where to go* — a settings file, a mounted
Secret, or nowhere durable — and one generic refusal would send a Kubernetes operator hunting through
`appsettings.json`; and the hundredth key minted in one day had neither its refusal nor its boundary
tested.

**Frontend, 56.19 % → 87.25 %.** The panel's `wasLeftBehind` filter survived every mutation including
replacement with `true`. That filter decides what an operator is shown: mutated, it lists all 47 moved
secrets instead of the one that needs action. The test suite only ever put a single unreadable secret in
the report, so the filter was never exercised against anything it should exclude. Also missing: the
clean case (success severity, no table), the failure paths, and the disabled state that stops somebody
starting a second pass by double-clicking.

One change was a simplification rather than a test. `SECRET_OUTCOME_WORDING` carried entries for
`Moved`, `Unmoved` and `MovedByAnotherWriter`, none of which the filter ever renders — dead strings with
mutants nothing could kill. The map is now typed to the three outcomes that ask somebody to do
something, and `wasLeftBehind` is a type guard that proves it.

## What is left alive, and why it stays

**Backend — 19 survived, 6 uncovered.** Eleven are message *fragments* in
`MintingNotPermittedException`, `GeneratedKeyRingStore` and `EncryptionController`. The load-bearing
phrases are asserted; blanking the connective text between them survives, and pinning every clause would
turn the tests into a transcript of the copy. Five are guard removals where another guard catches the
same call first. Four are the `oid` and `"unknown"` fallbacks in `WhoAskedForIt` for a principal with no
`sub` claim. One is the mid-walk cancellation check, where cancellation is still honoured one row later
by the token passed to the database call.

**Frontend — 13 survived, all decoration or equivalent.** Six MUI `sx` prop objects, two `variant`
strings, one `flexWrap`, one React `key` template, two hook dependency arrays, and one catch block that
sets state to the value it already holds.

## Not measured, and it should be said plainly

**The `NeedsProtecting` fix is outside the mutated scope.** It is the more dangerous of the two
credential-destroying defects this slice fixed — an ordinary Connection save was wrapping a secret it
could not read, converting a recoverable state into a permanent loss — and it lives in
`Data/LighthouseAppContext.cs`. That file is 640 lines of unrelated persistence, and Stryker.NET ignores
line spans, so scoping to the one method is not possible without generating thousands of mutants across
the whole context. It has three targeted tests behind it. It contributes nothing to the number above,
and the number should not be read as covering it.

## Reproducing

One after the other, never overlapping — both at once puts the frontend over the heap it needs and
returns a result that is all `Timeout`.

Backend, from `Lighthouse.Backend/Lighthouse.Backend.Tests/`:

```
dotnet stryker --config-file stryker-config.epic-5775-slice-03.json --output StrykerOutput-5778
```

Frontend, from `Lighthouse.Frontend/`:

```
NODE_OPTIONS=--max-old-space-size=8192 pnpm exec stryker run stryker-config.epic-5775-slice-03.json
```
