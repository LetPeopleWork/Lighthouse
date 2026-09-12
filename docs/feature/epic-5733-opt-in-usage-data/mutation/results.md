# Mutation testing — 5834 (Usage Data: consent, and one click that changes your mind)

Run 2026-09-12 against `main`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no cov | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **93.62 %** | 47 | 44 | 2 | 1 | 2 m 42 s |
| Frontend (StrykerJS 9.6.1) | **not measurable** | — | — | — | — | — |

Configs: `stryker.5834.backend.json`, `stryker.5834.frontend.json`, `vitest.stryker.mutation.ts`.

---

## Backend — gate met

| file | killed | survived | no cov |
| --- | --- | --- | --- |
| `UsageDataConsentRepository.cs` | 16 | 0 | 0 |
| `UsageDataConsentService.cs` | 20 | 2 | 0 |
| `UsageDataController.cs` | 7 | 0 | 1 |
| `Extensions/UrlSafeValue.cs` | 1 | 0 | 0 |

### What the first run found, and why it mattered

The first run scored **35.78 %** — 26 survivors. Every one was a **boundary or a direction**:
`LastSeenAt < staleBefore` flipped to `>`, `now - window` flipped to `now + window`,
`Decision == Granted` flipped to `!=`, `!refusalIsFinal` flipped to `refusalIsFinal`.

None of them changed a single HTTP response, because the acceptance tests are black box over HTTP
(DT-3) and run at one instant against rows created moments earlier. Nothing is ever stale, aged out,
or sitting on a threshold, so flipping a comparison is invisible to them. This is exactly what the
DISTILL band predicted when it said the store-level guarantees "need the entity and are authored in
DELIVER" — the tests were never written, and nothing noticed until mutation testing asked.

### Closed by this pass

Two new test classes, 26 tests, written against the survivor list rather than against the code:

- **`UsageDataConsentRepositoryTests`** (15) — every method with rows on *both* sides of its
  boundary. A grant one day old and one forty-five days old. A stamp ten hours stale and one five
  minutes fresh. A refusal, a withdrawal and a grant against the same revoke call. Three of them put
  a row **exactly on** the threshold, which is the only thing that separates `<` from `<=` and was
  the last cluster to fall.
- **`UsageDataConsentServiceTests`** (13) — the `willAskAgain` matrix as six explicit cases
  (Granted / Declined / Revoked × Premium / Community), the direction of the liveness window, that
  the throttle is a slice of the window rather than a multiple of it, and that a grant is still
  recorded when the identifier cannot be minted.

Two more went into `AppSettingServiceTest` for the identifier: that a second call keeps the existing
one, and that the save actually happens.

### Accepted survivors

| where | mutant | why it stays |
| --- | --- | --- |
| `UsageDataConsentService.cs:77` | `logger.LogError(…)` removed | The behaviour that matters — the grant is recorded anyway — has its own test. This line is diagnostics. |
| `UsageDataConsentService.cs:79` | its message emptied | Log narration. This repository already ignores this whole class wholesale, with written reasons, in its Stryker run. |

### Not mutated, and why — read this before trusting the headline

**`AppSettingService.cs` is excluded from `mutate`, and excluding it raised the score.** Both numbers:

- **93.62 %** over the four files this slice created.
- **55.05 %** with `AppSettingService.cs` included.

The difference is not test quality. That file is 275 lines of which roughly 45 are this slice's;
Stryker.NET ignores line ranges, so including it drags in **38 no-coverage mutants** from
pre-existing refresh-settings and survey-cadence paths that this slice never touched, and those count
against the score. Two of its four survivors (`UpdateRefreshSettingsAsync`, `GetSettingByKey`) are
likewise pre-existing.

The slice's own methods in that file are covered another way, and the tests were written **before**
the file was excluded rather than after: `EnsureUsageDataInstanceId` now has tests for both the mint
and the keep-what-exists path.

---

## Frontend — could not be measured, and the reason is not this feature

StrykerJS reported **3.26 %** for the four frontend files. That number is wrong and is not reported
as a result.

**The evidence it is a harness fault:**

1. `Ran 0.17 tests per mutant on average`, with `coverageAnalysis: "off"` — which means every test
   runs for every mutant. The per-mutant log lines read `Tests ran:` followed by nothing.
2. The dry run is fine: `Initial test run succeeded. Ran 45 tests in 3 seconds`. Tests are found;
   they just do not run against mutants.
3. `UsageDataDialog.tsx` scored **0 killed / 19 survived** while 31 passing tests exercise that
   component directly. A mutant that empties the collector's name fails `getByText(/PostHog/)` on
   contact.
4. The whole run took 39 s. 74 mutants × 45 tests cannot fit in 39 s.

**The control experiment.** Re-running the reference feature's own config
(`stryker.5611.frontend.json`, unchanged code) produces **54.55 %** today against the **85.71 %**
recorded in its own `results.md`. Same config, same code, different answer — so the regression is in
the toolchain, not in either feature. Its dry run now collects 7 tests where its six spec files hold
far more.

**Most likely cause:** `@stryker-mutator/vitest-runner ^9.6.1` against `vitest ^5.0.0`. The reference
run predates that vitest major, and a runner that cannot drive the new vitest would show exactly this
signature — dry run fine, per-mutant runs empty.

**This affects every future frontend mutation gate in this repository, not just this feature.** It
wants its own investigation and probably a runner upgrade; it should not be rediscovered per feature.

### What the broken run found anyway, and which was real

`UsageDataService.ts` reported 1 survivor and **14 no-coverage** mutants. That one is genuine and
was not an artefact: the file had no test at all. The backend endpoint tests cover the server side and
the hook tests mock the service out entirely, so nothing exercised it. **`UsageDataService.test.ts`
now exists** — 6 tests covering the header name each call sends, what each returns, and that
withdrawal names the token.

### Config note for the next feature

The skill's example invocation is `pnpm exec stryker run docs/feature/…` from `Lighthouse.Frontend/`,
which assumes a `docs` symlink inside the frontend. There is none — and there should not be, since
that symlink is a known hazard (Biome reformats the whole docs tree through it). The working path is
`../docs/feature/…`.
