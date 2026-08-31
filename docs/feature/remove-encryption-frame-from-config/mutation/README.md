# Mutation testing — remove-encryption-frame-from-config (Story #5875)

**Stack**: frontend (StrykerJS + Vitest) · **Date**: 2026-08-31
**Config**: `Lighthouse.Frontend/stryker-config.5875.json` (gitignored, per repo convention)
**Runner config**: `Lighthouse.Frontend/vitest.stryker.5875.config.ts` (gitignored)
**Raw report**: `stryker-5875.json`

Specs in the run: `EncryptionPanel.test.tsx`, `SystemSettingsTab.test.tsx`.

## Headline

| Scope | Score | Reading |
|---|---:|---|
| **`KEY_CUSTODY_WORDING` — the coverage this story relocated** | **100%** (5/5) | The whole point of the run. PASS. |
| `EncryptionKeyState.ts` (whole file) | 83.33% (5/6) | PASS. Sole survivor is out of scope — see below. |
| `EncryptionPanel.tsx:100-150` | 60% (3/5) | Both survivors are CSS. See below. |
| `SystemSettingsTab.tsx` (whole file) | 43.66% (31/71) | Pre-existing debt in untouched code. See below. |
| Reported total | 47.56% | **Not a verdict on this change** — see below. |

## Why the headline number is not the verdict

47.56% is a scope artifact. This story is a **deletion**: it adds no production code. To ask "did the
tests I moved still hold?", the run had to mutate the *surviving* surface — and mutating
`SystemSettingsTab.tsx` whole sweeps in blackout periods, optional features, terminology and refresh
rendering, none of which this story touches and none of which ever had a mutation baseline (the Epic
5775 run mutated only lines 31-54, 96-107 and 209 of that file — exactly the frame now removed).

The 80% gate applies to what the change is responsible for. That is `KEY_CUSTODY_WORDING`, at 100%.

## The result that matters

The story deleted `describe("secret encryption key")` from `SystemSettingsTab.test.tsx` and moved its
custody-wording assertions to `EncryptionPanel.test.tsx`. Mutation confirms the move lost nothing:

```
Killed  EncryptionKeyState.ts:45  ObjectLiteral   KEY_CUSTODY_WORDING -> {}
Killed  EncryptionKeyState.ts:46  StringLiteral   "the key published with the product" -> ""
Killed  EncryptionKeyState.ts:47  StringLiteral   "generated for this instance"        -> ""
Killed  EncryptionKeyState.ts:48  StringLiteral   "supplied by configuration"          -> ""
Killed  EncryptionKeyState.ts:49  StringLiteral   "supplied by a mounted secret file"  -> ""
```

Every phrasing a user reads is pinned from its new home. 5/5.

## Survivors, each with a reason

| Mutant | Verdict |
|---|---|
| `EncryptionKeyState.ts:30` — `EncryptionKeyStateSchema = z.object({…})` → `{}` | **Out of scope.** The zod schema is consumed by `EncryptionService.ts:25` and covered by `EncryptionService.test.ts`, which is deliberately not in this run's include set. Killed by a spec the run cannot see. |
| `EncryptionPanel.tsx:125` ×2 — `sx={{ flexWrap: "wrap" }}` → `""` / `{}` | **Not worth killing.** Pure layout. Asserting on `flexWrap` would test MUI, not behaviour. |
| `SystemSettingsTab.tsx` — 35 survived, 5 no-coverage | **Pre-existing.** All sit in optional-features state, the license fetch, the toggle handler and render props. None is encryption-related; none is code this story wrote. This story only removed lines from the file. |

## The mutation Stryker structurally cannot generate

A deletion has no code left to mutate, so no generated mutant can prove the frame stays gone. Two
checks were run by hand instead:

1. **Wording mutant** — changed one expected phrase in the relocated `it.each`. Exactly that case went
   red; reverted. The relocated test is not vacuous.
2. **Revert mutant** — restored `SystemSettingsTab.tsx` from `origin/main` (frame present) and ran the
   new spec. **Both absence tests failed**, as required. Restored; suite green.

   (Five unrelated tests also failed under that revert, because the old code renders on
   `keyState !== null` and an unstubbed `getKeyState()` resolves to `undefined`, crashing the tab. That
   is a property of the deleted code, not of the new tests.)

## Verdict

PASS for what this change owns. No test was added to chase a survivor; every survivor above is either
covered by a spec outside the run or is not behaviour.
