# Mutation testing — 5834 (Usage Data: consent, and one click that changes your mind)

Run 2026-09-12 against `main`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no cov | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **93.62 %** | 47 | 44 | 2 | 1 | 2 m 42 s |
| Frontend (StrykerJS 9.6.1) | **85.87 %** | 92 | 79 | 13 | 0 | 1 m 00 s |

The frontend number required pinning vitest back to `4.1.11` for the duration of the run. The repo
ships `5.0.0`, on which this gate cannot be measured at all — see **Frontend** below, which is worth
reading before the score is trusted or repeated.

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

## Frontend — 85.87 %, measured only by pinning vitest back

| file | killed | survived | no cov |
| --- | --- | --- | --- |
| `UsageDataService.ts` | 15 | 0 | 0 |
| `useUsageDataConsent.ts` | 47 | 2 | 0 |
| `UsageDataIndicator.tsx` | 7 | 1 | 0 |
| `UsageDataDialog.tsx` | 10 | 10 | 0 |

The first run reported **3.26 %**. That number was wrong, and the reason was not this feature.

### Why the first number was not a result

**The evidence it is a harness fault:**

1. `Ran 0.17 tests per mutant on average`, against `7.16` for the same config once it works. The
   per-mutant log lines read `Tests ran:` followed by nothing.
2. The dry run is fine: `Initial test run succeeded. Ran 45 tests in 3 seconds`. Tests are found;
   they just do not run against mutants.
3. `UsageDataDialog.tsx` scored **0 killed / 19 survived** while 31 passing tests exercise that
   component directly. A mutant that empties the collector's name fails `getByText(/PostHog/)` on
   contact.
4. The whole run took 39 s. 74 mutants × 45 tests cannot fit in 39 s.

**The cause, proven rather than suspected.** Commit `78a672ecd` raised vitest from `4.1.11` to
`5.0.0` on 2026-09-10. Story 5914's frontend mutation result was committed on 2026-09-09 — the day
before — at **94.96 %**.

Re-running 5914's own config against its own unchanged code, changing nothing but the vitest version:

| vitest | 5914's score | tests per mutant |
| --- | --- | --- |
| `5.0.0` (what the repo has today) | 7.19 % | 0.17 |
| `4.1.11` (what 5914 was measured on) | **94.96 %** | 16.14 |

94.96 % is the number in 5914's committed `results.md`, reproduced to two decimals. That is not a
correlation; it is the same measurement coming back once the version is put back.

Three other hypotheses were tested and disproven first, so they do not need retesting: the runner
version (`@stryker-mutator/vitest-runner` 9.6.1 vs 10.0.0 — identical behaviour), `related: false`,
and the `pool` / `isolate` / `server.deps` settings. None moved the number.

**Nothing warns.** `@stryker-mutator/vitest-runner` declares `peerDependencies: { "vitest": ">=2.0.0" }`,
so the install is considered valid, the dry run succeeds, and the tool reports a plausible-looking low
score instead of failing. A frontend mutation gate run on this repository today returns a number that
looks like weak tests and is actually an empty test run.

**This blocks the frontend gate repo-wide, not just this feature.** The three options — pin vitest
back to 4.x, accept the gate as blocked until the runner supports vitest 5, or pin plus report
upstream — trade a working test suite against a working mutation gate and are the maintainer's call,
not a decision to be made inside a feature's mutation pass.

### What the real run found

Pinned to `4.1.11`, the first honest measurement was **45.65 %**, and it went up in three steps:

| | score | what changed |
| --- | --- | --- |
| first honest run | 45.65 % | — |
| + include fix | 61.96 % | `UsageDataService.test.ts` was missing from `vitest.stryker.mutation.ts`'s `include` list, so its 6 tests never ran and its file showed 14 no-coverage mutants. The file went to 100 %. |
| + 13 hook and dialog tests | 82.61 % | below |
| + 2 corrected tests | 85.87 % | below |

**Closed by this pass — 13 new tests, written against the survivor list:**

- **The withdrawal condition** (`next === "declined" && decision === "Granted" && token`) held five
  survivors. Each clause is load-bearing and dropping any one sends the wrong request, but only the
  happy path had a test. Three cases now cover the rest: a token held with no prior grant, a grant
  reaffirmed rather than withdrawn, and a grant that belongs to some other browser.
- **The hourly refresh.** Nothing asserted the interval at all, so `60 * 60 * 1000` could become
  `60 / 60 * 1000` unnoticed — a tab would re-ask every second. Two tests with fake timers: nothing
  at 59 minutes, a second fetch at 60, and no further fetch after unmount.
- **The initial state, the dialog lifecycle and the failure banner.** Whether the dialog closes on
  success, whether an earlier failure clears, and whether `closeDialog` closes anything were all
  unasserted. The dialog's `failedToRecord` had no test of any kind — both the alert and its absence
  are now covered.

**Two of those tests were themselves wrong, and mutation testing is what said so.** Both passed on
the first try and neither could have failed:

- *"falls back to not knowing when the state cannot be fetched"* asserted `"unknown"` after a
  rejected fetch — but `"unknown"` is also the initial state, so it asserted nothing. It now lets the
  hook learn the state first and then lose it, which is the case that actually distinguishes the two.
- *"treats a browser that cannot be asked for a token as one that holds none"* asserted the token
  read as `null` while storage was empty, where `null` is simply the normal answer. It now writes a
  token first, so `null` can only come from the throwing path. That also exposed a second fault:
  `vi.spyOn(Storage.prototype, "getItem")` never intercepted anything under jsdom — the real value
  came back. Spying on the `localStorage` instance works.

A survivor that turns out to be a test which cannot fail is the most valuable kind, and the reason
the score is quoted after the fix rather than before it.

### Accepted survivors

| where | count | why they stay |
| --- | --- | --- |
| `UsageDataDialog.tsx`, `UsageDataIndicator.tsx` | 11 | MUI `sx` / `style` props: `{ mb: 2 }` emptied, `"disc"` and `"list-item"` blanked. These change spacing and bullet shape, nothing a DOM assertion can see and nothing that alters what the dialog says. Pinning them would mean asserting on style objects, which breaks on every visual tweak and protects nothing. |
| `useUsageDataConsent.ts:80,87` | 2 | React dependency arrays emptied. Killing these means re-rendering with a swapped API service mid-test purely to observe a stale closure — a scenario the application never produces, since the service is created once at composition. |

### Config note for the next feature

The skill's example invocation is `pnpm exec stryker run docs/feature/…` from `Lighthouse.Frontend/`,
which assumes a `docs` symlink inside the frontend. There is none — and there should not be, since
that symlink is a known hazard (Biome reformats the whole docs tree through it). The working path is
`../docs/feature/…`.
