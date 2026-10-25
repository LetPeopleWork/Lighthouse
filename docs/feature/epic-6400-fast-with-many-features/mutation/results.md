# Mutation testing — 6404 (Epic 6400 slice 01: the forecast lane and the Features band)

Gate is an 80 % kill rate on each stack the slice touched.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | pending | | | | | |
| Frontend (StrykerJS 10.0.0), first run | 93.26 % | 89 | 83 | 6 | 0 | 5 m 38 s |
| Frontend, incremental re-run after the kill round | **96.63 %** | 89 (6 reused) | 86 | 3 | 0 | 5 m 39 s |

Configs: `stryker.6404.frontend.json` and `vitest.stryker.mutation.ts`. The vitest config is copied into
`Lighthouse.Frontend/` (gitignored there); the run is from `Lighthouse.Frontend/`:

```
TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/epic-6400-fast-with-many-features/mutation/stryker.6404.frontend.json --incremental
```

`coverageAnalysis: "perTest"`, incremental file `reports/mutation/stryker-incremental-6404.json`
(gitignored), concurrency 2, `inPlace`, `disableTypeChecks: false`, root-anchored `ignorePatterns`.
The re-run reused only 6 results because the kill round edited the spec that covers nearly every mutant.

## Backend

Pending — recorded separately by the backend run.

## Frontend

Frontend code run against `main` @ `6a6e2ea81` (the slice's last frontend commit).

### What was mutated

Whole files, because the slice wrote them: `src/models/UsageData/featuresHeldOf.ts` and
`src/services/UsageData/usageDataFeaturesHeld.ts`.

Line ranges, one entry per range, from `git diff -U0 64289af73^..HEAD` (import-only hunks skipped):
`usageDataEvents.ts:77-79`, `:92-109`, `:111-119`, `:122-132`, `:163-163`; `TeamDetail.tsx:131-136` and
`PortfolioDetail.tsx:100-108` (the `useFeaturesHeldForUsageData` call on each page).

The log confirms 5 files instrumented with 89 mutants, and every file appears in the per-file table.

### Not mutated

`usageDataBuffer.ts` and `UsageDataService.ts`: each carries one optional `featuresHeld` field through
untouched. The band tests assert that field on what is handed in, so a broken pass-through fails them.

### Suites run

`featuresHeldOf.test.ts`, `usageDataEvents.featuresHeld.test.tsx` (renders the real Team and Portfolio
detail pages), `usageDataEvents.test.ts`, `usageDataReporter.test.ts` and
`SystemSettingsTab.usageDataHandIn.test.tsx`; the last two mount the tab-opening detector too.

### Per file

| file | first run | after kill round | killed | survived |
| --- | --- | --- | --- | --- |
| `featuresHeldOf.ts` | 95.65 % | 95.65 % | 22 | 1 |
| `usageDataFeaturesHeld.ts` | 91.89 % | 97.30 % | 36 | 1 |
| `usageDataEvents.ts` (slice lines) | 89.47 % | 94.74 % | 18 | 1 |
| `TeamDetail.tsx` (hook call) | 100.00 % | 100.00 % | 5 | 0 |
| `PortfolioDetail.tsx` (hook call) | 100.00 % | 100.00 % | 5 | 0 |

### Closed by this pass

Three tests added to `usageDataEvents.featuresHeld.test.tsx`. Each was also checked by applying its
mutant by hand: the new test, and only it, fails.

- **`usageDataEvents.ts:126`** `if (isStillSending.current)` → `if (true)`. "sends nothing that was still
  waiting for its Team once the person withdraws": a withdrawal empties the buffer, and without the guard
  the opening still waiting for its Team went into it afterwards and left at the next hand-in, because the
  browser keeps its token after saying no.
- **`usageDataFeaturesHeld.ts:75`** `loadedId !== undefined &&` → `true &&`. "goes as Unknown at the next
  hand-in when the Portfolio fails to load, while the person stays": without the clause a page whose
  owner failed to load never hands in Unknown, so its opening waits until the person leaves. The earlier
  failed-load test hid the tab, and hiding sends Unknown by another route.
- **`usageDataFeaturesHeld.ts:19`** the key `${kind}:${id}` → `""`. "never takes the band a Portfolio handed
  in for a Team page, nor for another Portfolio": with one shared key every page read whichever band was
  handed in last.

### Accepted survivors

- **`featuresHeldOf.ts:19`** `count === undefined` → `false`. Equivalent: `Number.isInteger(undefined)` is
  false, so the next clause returns Unknown for the same input. The clause is there for the type checker.
- **`usageDataEvents.ts:96`** `stopWaiting?.()` → `stopWaiting()`. Unreachable: `noticeWith` is only
  reached through the listener, which is registered after `stopWaiting` is assigned, and the first call
  removes the listener and clears the fallback, so it never runs a second time.
- **`usageDataFeaturesHeld.ts:43`** `key === undefined ? undefined : held.get(key)` → `held.get(key)`.
  Equivalent: `Map.get(undefined)` returns `undefined` when no key is `undefined`, and none ever is.
