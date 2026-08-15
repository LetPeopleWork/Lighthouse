# Epic #5775 slice 01 — mutation testing results (2026-08-15)

Strategy `per-feature` (CLAUDE.md), gate ≥ 80 % kill rate on the surface this slice changed.
**Both stacks pass.**

| stack | changed surface | whole mutated files | killed | survived | no coverage | config |
|---|---|---|---|---|---|---|
| backend (Stryker.NET 4.14.1) | **94.88 %** (204 / 215) | 84.57 % (296 / 350) | 296 | 39 | 15 | `stryker.5777.backend.json` |
| frontend (StrykerJS) | **94.59 %** (35 / 37) | same | 35 | 2 | 0 | `stryker.5777.frontend.json` |

The two backend numbers differ because Stryker.NET cannot scope below a whole file (see the traps
below), so the run also mutates code these files already carried. The **changed surface** column counts
only mutants landing on lines this slice touched, taken from `reports/mutation-report.json` and the
`git diff 3a79a8ec0..HEAD` line ranges. It is the number the gate is read against; the whole-file
number is reported beside it so nothing is hidden. The frontend config scopes by line span, which
StrykerJS does honour, so its two numbers are the same.

Scope, proven from the run rather than from the config: **15 071 mutants created, 14 736 skipped, 335
tested.** The created-count is a pre-filter figure covering the whole project and means nothing on its
own.

## Per file, backend

| file | score | killed | survived | no cov |
|---|---|---|---|---|
| `Models/Encryption/EncryptionKey.cs` | 100.00 % | 10 | 0 | 0 |
| `Services/…/Encryption/ConnectionSecrets.cs` | 100.00 % | 7 | 0 | 0 |
| `Services/…/Encryption/EncryptionKeyRingHolder.cs` | 100.00 % | 3 | 0 | 0 |
| `Services/…/Encryption/UnreadableSecretException.cs` | 100.00 % | 7 | 0 | 0 |
| `API/DTO/WorkTrackingSystemConnectionOptionDto.cs` | 100.00 % | 5 | 0 | 0 |
| `Services/…/Encryption/SecretEnvelope.cs` | 95.24 % | 80 | 3 | 1 |
| `Services/…/Encryption/SecretStateClassifier.cs` | 94.23 % | 49 | 3 | 0 |
| `Models/Encryption/EncryptionKeyRing.cs` | 93.33 % | 14 | 1 | 0 |
| `Services/Implementation/CryptoService.cs` | 93.33 % | 14 | 1 | 0 |
| `API/WorkTrackingSystemConnectionsController.cs` | 77.55 % | 38 | 8 | 3 |
| `Services/…/Update/UpdateServiceBase.cs` | 68.52 % | 37 | 13 | 4 |
| `Services/…/Update/PortfolioUpdater.cs` | 68.97 % | 20 | 6 | 3 |
| `Services/…/Update/TeamUpdater.cs` | 60.00 % | 12 | 4 | 4 |

The last four are shared files this slice added a few lines to. Their whole-file scores are dominated
by log-message and refresh-interval mutants that predate this work; of the survivors, only four sit on
a line this slice wrote, and all four are judged below.

## What this run was for

**A secret that cannot be read has to say so, and the classifier is the only thing standing between a
wrong key and a credential.** The first run scored 72.57 % and put thirteen survivors on
`SecretStateClassifier`, every one of them in the logic that separates a real legacy secret from
well-formed rubbish. Those are not metrics. A legacy CBC blob carries no authentication tag, so the
decrypt always *succeeds* — the wrong key is caught only by what it produces, and each surviving mutant
was a way for that catch to be dropped without a test noticing:

| mutant | what it would have done |
|---|---|
| `padding < 1` → `<= 1` | refused a credential one character short of a block, whose PKCS7 padding is a single byte |
| `padding > 16` → `>= 16` | refused a credential that fills a block exactly, padded with a whole extra block |
| `padding > decrypted.Length` → `>=` | refused an empty stored credential, which is nothing but padding |
| the two `\|\|` → `&&` rewrites of that guard | **accepted** a value claiming twenty bytes of padding — out of range for a 16-byte block |
| the padding-consistency `return false` → `true` | **accepted** a block whose padding bytes disagree with the length they claim |
| `replaceInvalidSequences: false` → `true` | **accepted** bytes that are not UTF-8, silently rewriting them as replacement characters and calling the result a secret |
| the UTF-8 `return false` → `true` | same, without even the rewrite |
| `decoded.Any(char.IsControl)` → `.All(…)` | **accepted** text riddled with control characters, rejecting only text that is *entirely* control characters |
| the control-character `return false` → `true` | same |

All ten are now killed by seven cases in `SecretStateClassifierTests`. Four of them write a CBC blob
with the padding left off (`CbcBlobOfRawBytes`), which is the only way to hand the classifier a value
that decrypts cleanly under a key it holds and still is not a secret — exactly what a wrong key looks
like from the inside. Three read back a real legacy blob sitting on a padding boundary, to prove the
tightened checks do not refuse an ordinary credential.

**The envelope's boundaries were unpinned in three places**, all now killed: a key id of exactly the
greatest allowed length, a key id containing `z` (the far end of the allowed letter range), and an
**empty** credential — whose ciphertext field is the authentication tag and nothing else, and which a
`< TagLength` → `<= TagLength` mutant would have refused to parse, sending an empty stored value down
the legacy path to be reported as something it is not.

**Key identity was unpinned.** `EncryptionKey.Equals` dropped to 40 %: two keys sharing a name but not
their material compared equal, and `Equals(null)` threw rather than returning false. Both matter for
rotation, where a ring silently accepting the wrong material under a name secrets were already written
under would make every one of those secrets unreadable with nothing to say why. `EncryptionKey` and
`EncryptionKeyRingHolder` are now at 100 %.

**The reason an operator reads was unpinned.** `BuildUnreadableSecretReason` had seven survivors on
lines this slice wrote: the message would still have passed its tests while saying `The stored ` and
then nothing (when the failed read is an OAuth refresh token, which is not a connection option and so
names no field), while claiming a secret was written under an encryption key called `''`, and while
losing the sentence that tells the operator what to do. Six cases now pin it, including the whole
closing sentence.

## The `EncryptSecrets` guard — verified by hand, not by Stryker

`Data/LighthouseAppContext.cs` is **deliberately not in the `mutate` list**. Stryker.NET has no
granularity below a whole file, and 580 of that file's 632 lines are EF model configuration this slice
never touched; including it would have measured someone else's code and reported a number about this
one. The guard was mutated by hand instead, five mutants applied with `sed` and reverted after each:

| mutant | outcome |
|---|---|
| `is not { State: Envelope }` → `is { State: Envelope }` | **Killed** — 5 of 7 `SecretEncryptionOnSaveTests` fail |
| `option.IsSecret && NeedsProtecting(…)` → `\|\|` | **Killed** — 2 fail |
| the two OAuth-token `&& NeedsProtecting(…)` → `\|\|` | **Killed** — 1 fails |
| `NeedsProtecting` → `return true` | **does not compile** (S2325, S1172) |
| `NeedsProtecting` → `return false` | **does not compile** (S2325, S1172) |

Three of three compilable mutants killed. The two that do not compile are the analyzer settings doing
Stryker's job: a guard that ignores its argument stops building.

## Surviving mutants on lines this slice wrote — backend (11)

Ten of the eleven are equivalent or copy-only. One is a named gap.

**Genuinely equivalent — the mutant cannot change behaviour:**

- `CryptoService.cs:23` and `SecretEnvelope.cs:50` — `ArgumentNullException.ThrowIfNull(…)` removed.
  Both arguments are dereferenced two lines later by something that throws the same exception type
  (`new SecretStateClassifier(keyRingHolder)`, `Encoding.UTF8.GetBytes(plainText)`). A test was written
  for the second one and it still survived; that survival *is* the evidence.
- `SecretEnvelope.cs:163` and `:170` — `TryDecode` returning `true` on a field that is not base64url or
  fails to decode. `decoded` stays empty, and both call sites then reject on length (a nonce that is
  not 12 bytes, a ciphertext shorter than the tag), so `TryParse` still returns false. `:170` is
  additionally unreachable: `IsBase64Url` has already filtered.
- `SecretStateClassifier.cs:64` — `Prepend(ActiveKey)` → `Append`. This changes which key is *tried*
  first, not which one succeeds. For the result to differ, two distinct keys would have to decrypt the
  same blob to printable UTF-8.
- `SecretStateClassifier.cs:86` — `storedValue.Length / 4 * 3` → `* 4 * 3`. The buffer is an upper
  bound handed to `Convert.TryFromBase64String`; over-allocating it costs memory and changes nothing.
- `SecretStateClassifier.cs:139` — `plainText = string.Empty` → some other string. The out-parameter is
  only read when the method returns true, and the success path overwrites it.

**Copy inside an exception nobody reads twice** — the refusal itself is asserted by type, and pinning
the wording would assert the wording rather than the behaviour:

- `EncryptionKeyRing.cs:17` — the message refusing an empty ring.
- `SecretEnvelope.cs:54` — the message refusing a malformed key id, already asserted over eight
  invalid ids.

**Logging** — `TeamUpdater.cs:73`, a Debug line.

**The one real gap, named rather than fixed:** `WorkTrackingSystemConnectionsController.cs:140`, the
failure code `"secret_cannot_be_read"` → `""`. Validation returns a machine code beside the human
message and the field name; the tests assert the message and the field name, and nothing asserts the
code. A UI that ever switched on it would not notice it change. Left as a follow-up because the code is
not yet consumed anywhere — the notice and the field marking both key off `fieldName`.

Forty-three further survivors sit outside this slice's lines, dragged in by whole-file scope: the two
updaters' refresh-interval arithmetic and log lines, and the controller's create and patch helpers. Nine
mutants across the updaters are `Ignored` by `// Stryker disable` annotations that predate this work.

## Surviving mutants — frontend (2)

- `SecretHandlingNotice.tsx:5` — `SECRET_HANDLING_NOTICE_TEST_ID` → `""`. **Unkillable by
  construction**: the specs import the same constant to query by, so the mutant changes both sides of
  the comparison.
- `ModifyConnectionSettings.tsx:561` — `<Grid size={{ xs: 12 }}>` → `{}`, the grid column span the
  notice sits in. A test for it would assert a layout breakpoint, which is an implementation detail of
  the page rather than anything the notice promises.

The frontend run also turned up one real hole, now killed: nothing asserted the helper text on an
OAuth-locked credential field, so `lockOAuthField` could be forced true and every ordinary connection
would tell its owner the field was locked after an OAuth handshake that never happened. Two specs now
pin both directions.

## The traps, for whoever re-runs this

1. **Stryker.NET silently ignores line-span `mutate` patterns.** `"**/Foo.cs{72..94}"` matches nothing,
   reports no error, and yields a clean score describing none of your code. Use whole-file entries and
   triage survivors by line — or, where a file is mostly unrelated (as `LighthouseAppContext.cs` is),
   leave it out and mutate the few lines by hand.
2. **`15 071 mutants created` is a pre-filter count and is normal.** Stryker.NET injects into every
   file, then filters, then compiles, so a correctly scoped run still prints the whole-project figure
   and logs `Safe Mode!` warnings about files you never named. Scope is proven about two minutes later
   by `N total mutants are skipped / M will be tested`.
3. **A missing `test-case-filter` is the real cost.** Without one, roughly 4 900 tests run under
   `perTestInIsolation` before mutation even starts. The filter here selects 394 tests and excludes
   `Integration/Containers`, whose testcontainers-backed round-trip needs Docker and proves column
   width rather than any decision.
4. **Run the two stacks sequentially.** A backend run overlapping a frontend one at concurrency 6 once
   produced 100 % `Timeout`, which is not a result.
5. **StrykerJS runs `inPlace: true` here** — the repo's TypeScript no longer exports what its sandbox
   preprocessor calls, so sandbox mode dies at instrumentation. In-place mode mutates the real working
   tree. Set **`"disableTypeChecks": false`** (its default, `true`, prepends `// @ts-nocheck` to every
   matched file and one OOM once left 661 files modified), keep `coverageAnalysis: "off"` and
   `concurrency: 2`, and narrow the vitest `include` in `vitest.stryker.mutation.ts` to the specs that
   cover the mutated files. Commit before running it; recovery is `git checkout -- Lighthouse.Frontend/`.
6. **StrykerJS *does* honour line spans**, unlike Stryker.NET — but take the spans from the code, not
   from `git diff`. The first frontend run scored 60.78 % because the diff hunks for
   `ModifyConnectionSettings.tsx` cover a large block of JSX that was **moved**, not written: MUI `sx`
   and `size` props and optional chaining on props, none of which any test should pin. Narrowing to the
   regions this slice actually authored took the same suite from 60.78 % to 83.78 % without a single
   new test, and the number finally described the feature.
7. **`vitest.stryker.mutation.ts` lives in `Lighthouse.Frontend/` and is gitignored** by the repo
   convention that Stryker configs are local tooling, so it cannot be checked in beside this file.
   Recreate it as a copy of `vitest.config.ts` with `include` narrowed to the three specs that cover
   the mutated files:

   ```ts
   include: [
       "src/components/Common/Connection/SecretHandlingNotice.test.tsx",
       "src/components/Common/Connection/ModifyConnectionSettings.test.tsx",
       "src/components/Common/Connection/CreateConnectionWizard.test.tsx",
   ],
   ```
