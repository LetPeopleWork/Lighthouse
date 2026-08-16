# Epic #5775 slice 06a (Story #5791) — mutation testing results (2026-08-16)

| stack | score on the scoped surface | killed | survived | config |
| --- | --- | --- | --- | --- |
| backend (Stryker.NET 4.16.0) | **96 %** (48 / 50 tested) | 48 | 2 → 0 after the follow-up | `stryker.5791.backend.json` |
| frontend (StrykerJS) | not run — see below | — | — | — |

Scope: the three production files this slice added logic to — `ReferencedKeyIds.cs`,
`ConfiguredKeyRingSource.cs`, `EncryptionStateDto.cs`. `EncryptionPanel.tsx` and `StartupBanner.cs` are
deliberately outside it; see below.

Stryker's headline number was **84.21 %**, which folds uncovered mutants into the denominator. Of the
mutants actually tested, 48 of 50 were killed on the first run.

## The two survivors, and why they were worth killing

Both were in `ConfiguredKeyRingSource`, and both were cheap:

- **`string.IsNullOrWhiteSpace(suppliedRing)` weakened to `suppliedRing != ""`.** That is a real
  behaviour, not a formality: a setting holding nothing but spaces has not answered anything — the
  resolution passes straight over it — so naming it would send an operator to edit a line that is doing
  nothing. Now covered across every combination of the three settings, including the one where they all
  hold only spaces.
- **The argument guard on the setting-name speller.** A panel or a refusal quoting an empty setting name
  teaches an operator nothing and reads as a bug in the product.

Both killed; the scoped surface is at zero survivors.

## What is deliberately not in scope, and why

**`EncryptionPanel.tsx` — no frontend run.** Almost every line of this slice's frontend change is a
sentence: which words the summaries use, which button is emphasised, what the header says. Those are
pinned by 37 assertions in `EncryptionPanel.test.tsx` that read the rendered text, and a mutation score
over string literals measures how many of them a test quotes rather than whether the screen is right.
The two pieces of actual logic — `movingWouldAchieveSomething` and the non-zero filter in the summary —
are each covered by tests from both sides. The remaining risk on this file is a wrong sentence, and no
mutant finds that.

**`StartupBanner.cs`** was scored in slice 05b and is unchanged here except for the custody wording,
which is asserted per custody in `StartupBannerEncryptionKeyLineTest`.

**`EncryptionController.cs`** is wiring: it reads two settings and hands them to a constructor. Its
behaviour is asserted over HTTP in `EncryptionControllerTests`, which is where a mistake in it would
actually show.
