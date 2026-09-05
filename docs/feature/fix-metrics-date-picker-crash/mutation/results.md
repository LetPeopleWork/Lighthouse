# Mutation testing — 5915 (Adding a 0 in the Date selection Metrics view causes an exception)

Run 2026-09-05 against `main` @ `8e9800528`. Gate is 80 % kill rate on every stack with changed files.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 9.6.1) | **92.13 %** | 267 | 240 | 21 | 6 | 7 m 47 s |
| Backend (Stryker.NET) | N/A | — | — | — | — | — |

**Backend is N/A, not skipped:** this bug is entirely frontend. `git diff e8bae4995^..HEAD` touches no
file under `Lighthouse.Backend/`, so there is nothing for Stryker.NET to mutate.

Configs: `stryker.5915.frontend.picker.json`, `stryker.5915.frontend.trend.json`,
`stryker.5915.frontend.baseview.json`, with `vitest.stryker.picker.ts`, `vitest.stryker.trend.ts`,
`vitest.stryker.baseview.ts`. Reports: `stryker-5915-picker.json`, `stryker-5915-trend.json`,
`stryker-5915-baseview.json`.

## Why three runs instead of one

The first attempt used a single config over all ten specs and was killed by the OS for memory
pressure. `coverageAnalysis` is `off`, so Stryker reruns **every included spec for every mutant**, and
this bug's scope drags in `BaseMetricsView.test.tsx` — 6382 lines, the largest spec in the repo and
88 % of the include set. Earlier features' mutation runs never carried a spec near that size.

Splitting by subject changes no arithmetic: the same 267 mutants over the same production lines, in
three reports instead of one. `BaseMetricsView` gets a run to itself; the other two are light.
Concurrency was also dropped from 2 to 1.

| run | mutates | wall clock | score |
| --- | --- | --- | --- |
| picker | `DateRangeSelector.tsx`, `isValidDate.ts`, `localDate.ts`, `DashboardHeader.tsx` (2 ranges) | 5 m 11 s | 85.27 % |
| trend | `blockedTrend.ts`, `usePbcOverTime.ts`, `usePercentilesOverTime.ts` | 2 m 03 s | 98.46 % |
| baseview | `BaseMetricsView.tsx:1225-1240` | 33 s | 100.00 % |

## Frontend

| file | tested | killed | survived | timeout | score |
| --- | --- | --- | --- | --- | --- |
| `DateRangeSelector.tsx` | 84 | 67 | 17 | 0 | 79.76 % |
| `localDate.ts` | 37 | 35 | 2 | 0 | 94.59 % |
| `usePbcOverTime.ts` | 23 | 19 | 1 | 3 | 95.65 % |
| `usePercentilesOverTime.ts` | 23 | 19 | 1 | 3 | 95.65 % |
| `blockedTrend.ts` | 84 | 84 | 0 | 0 | 100.00 % |
| `BaseMetricsView.tsx` (lines 1225-1240) | 8 | 8 | 0 | 0 | 100.00 % |
| `isValidDate.ts` | 5 | 5 | 0 | 0 | 100.00 % |
| `DashboardHeader.tsx` (lines 51-54, 141-142) | 3 | 3 | 0 | 0 | 100.00 % |

`BaseMetricsView.tsx` is mutated by line range: 16 of its 1922 lines belong to this fix, and mutating
the whole file would bury the change's score under untouched code.

### Closed by this pass

The first run scored **74.91 %**. Every kill below was verified by applying the mutation to the
source, watching the suite go red, and reverting — 46 mutants, one run each.

- **Draft-sync effect** (`DateRangeSelector.tsx:55-59`, 6 of 7). No test had ever changed the `value`
  prop while the field was mounted, so deleting the effect, emptying the updater, or flipping its
  ternary were all observationally identical. Now pinned by a rerender with a new start date, a
  half-typed draft being overridden, and an emptied field (the last is the only case where the draft
  is genuinely null, which is what pins the optional chaining).
- **Range-guard boundaries** (`:66`, `:70`). `<` → `<=` and `>` → `>=` both survived because no test
  finished an edit exactly on `minDate` or `maxDate`. Under the mutants a one-day range — start equal
  to end — is silently refused. Two tests now land exactly on each bound.
- **The revert** (`:80`). Deleting `else { setDraft(value); }` left the whole repo green while the
  field displayed `00.07.2026` forever, which is the reporter's own symptom. The existing test
  asserted only that nothing was reported; a new one asserts the last working date is back in the
  field.
- **The calendar path** (`:113-120`, several with no coverage at all). No test had ever opened the
  calendar popper. One test now picks a day and asserts a single commit, which also killed the
  `useState(false)` initial-state mutant that nothing had reached.
- **Locale format** (`:15`). Under the runner's own en-US locale, `getLocaleDateFormat` returning
  `undefined` is byte-identical to the real thing — the adapter's fallback is `MM/dd/yyyy` and so is
  the function's result. The test pins the locale to `de-DE` through an `Intl` spy so the assertion
  can actually fail.
- **Trend direction** (`blockedTrend.ts`, all 10 survivors plus the metric label). The sign of the
  change and the equal-values case are now explicit, along with two snapshots sharing a timestamp.
- **`parseLocalDate` validation** (`localDate.ts`, 8 of 10). Every rejected input in the suite failed
  all three conjuncts at once, so dropping any one survived; the regex anchors survived because
  nothing fed valid-looking junk around a real date. One input per conjunct, plus anchored-junk cases.
- **The cancel flag** (both over-time hooks, all 8). Driven by changing the owner while the range
  stays put, so both requests share a cache key and the stale response overwrites the fresh one under
  the mutant — one team's limits displayed under another team's name.

### Accepted survivors

**Fifteen MUI styling mutants in `DateRangeSelector.tsx`** — lines 106 (×2), 122, 123, 124, 130, 131,
134, 135, 136, 169, 170, 171, 172, 174: `sx={{…}}` → `{}`, `width: "100%"` → `""`, `size: "small"` →
`""`, `fullWidth: true` → `false` and similar. They change only appearance. Asserting on them would
pin styling into the test suite and detect no defect. This is the whole of the gap between
`DateRangeSelector.tsx`'s 79.76 % and the rest of the feature.

**Two log-message strings** — `usePbcOverTime.ts:76` and `usePercentilesOverTime.ts:74`, the
`console.error` text. The tests assert a failure surfaces to the user, not the wording of a console
line.

**Four equivalent mutants** — the mutation cannot change observable behaviour:

- `DateRangeSelector.tsx:57`, ternary → `false`. It differs only when both operands denote the same
  instant. The draft is read only as the picker's `value` and through `getTime()`/`<`/`>`, all
  time-based, so the mutant substitutes a different object with an identical time. The only
  consequence is one extra render.
- `DateRangeSelector.tsx:99`, deps `[]` → `["Stryker was here"]`. React compares deps element-wise
  with `Object.is` and the literal is identical on every render, so the effect still runs once on
  mount and cleans up on unmount. The array length never changes between renders, so not even the
  development warning fires.
- `localDate.ts:42`, dropping `parsed.getMonth() === month - 1`. For that conjunct to be the only
  false one you would need a constructed date in the same year and on the same day-of-month but in a
  different month. A month index outside 0-11 always moves the year; a day outside the month's range
  always moves both the month and the day. No such input exists.
- `localDate.ts:43`, dropping `parsed.getDate() === day`. Symmetric: `getDate() !== day` requires an
  overflow, and every overflow also moves the month, so the month check has already rejected it.

### Not mutated

`DashboardHeader.tsx` outside lines 51-54 and 141-142, and `BaseMetricsView.tsx` outside lines
1225-1240 — untouched by this fix. The two new test files that drive the real picker
(`DateRangeSelector.keyboard.test.tsx`, `DashboardHeader.popover.test.tsx`) are test code and are
never mutation targets.

## Operational notes

- `inPlace: true` mutates the working tree. `git status` was checked after every run: no mutant, no
  `@ts-nocheck` and no `Stryker was here` marker was left in `src`.
- Stopping a background agent does **not** stop scripts that agent had already launched. A leftover
  mutation script kept running against this checkout between runs and left two production files
  mutated mid-run. Check for stray processes before starting a run, not only after.
