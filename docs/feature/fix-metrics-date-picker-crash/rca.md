# Root Cause Analysis — ADO Bug #5915

**Title:** Adding a `0` in the date selection on the Metrics view causes an exception
**Date:** 2026-09-04
**Method:** Toyota 5 Whys, multi-causal, evidence at every level
**Commit analysed:** `853a31d21` (main, clean tree)

---

## 1. Problem Statement (scoped)

**In scope.** Typing a digit into either MUI-X date field inside the Metrics dashboard's
date-range popover (Team Metrics and Portfolio Metrics alike) makes the whole application
unmount and replaces it with the `AppErrorBoundary` fallback. The trigger is any keystroke
that leaves a *complete but unparseable* date in the field — in practice the leading `0`
of a two-digit day or month, i.e. the first character of a perfectly ordinary entry such
as `05.07.2026`.

**Out of scope (verified unrelated).**
- The backend UTC anchoring work (Bug #5567) — no backend call is reached; the throw is
  synchronous, during render, before any fetch.
- The metrics fetch gate (Bug #5571) — `activeFetchKeys` is monotonic and behaves
  correctly; it is a *victim* of the bad date, not a cause.
- The React #185 unstable-`new Date()` dependency class — no loop occurs; a single throw
  unmounts the tree.

**Three distinct symptoms named by the reporter:**

| # | Symptom |
|---|---------|
| (a) | The crash / error screen |
| (b) | No fallback to the last working date |
| (c) | Commit-on-every-keystroke → many unnecessary reloads |

---

## 2. Evidence Base

All evidence below was produced against this checkout. The two throwaway probe specs were
run and then deleted; the tree is clean.

### E1 — `Invalid Date` is truthy and poisons three different sinks

Node probe against the repo's own `node_modules`:

```
truthy? true | instanceof Date: true | getTime: NaN
date-fns format THROWS: RangeError: Invalid time value
toISOString THROWS: RangeError: Invalid time value
formatLocalDate(invalid) -> NaN-NaN-NaN
dayjs isValid: false | invalid.isAfter(now): false
date-fns version: 4.4.0
mui-x version: 9.0.0
```

Three consequences, all load-bearing:
- `!invalidDate` is **false**, so every `if (!date) return;` guard in the codebase is a no-op
  against it.
- `date-fns` `format()` and `Date.prototype.toISOString()` **throw**.
- `formatLocalDate()` **does not throw** — it silently emits `NaN-NaN-NaN`.

### E2 — MUI-X v9 publishes `Invalid Date` through `onChange`, by design

`node_modules/@mui/x-date-pickers/internals/hooks/useField/useFieldState.js:282-288`:

```js
/**
 * If all the sections are filled but the date is invalid and the previous date is valid or null,
 * Then we publish an invalid date.
 */
if (newActiveDateSections.every(sectionBis => sectionBis.value !== '') && (activeDate == null || adapter.isValid(activeDate))) {
  setSectionUpdateToApplyOnNextInvalidDate(newSectionValue);
  return publishValue(fieldValueManager.updateDateInValue(value, section, newActiveDate));
}
```

and again at `:295-299` ("publish the date as `newActiveDate` to prevent error state
oscillation"). Pressing `0` in the day section is *not* skipped:
`useFieldCharacterEditing.js:173` passes `skipIfBelowMinimum: false` on the first keypress
of a section, so `cleanDigitSectionValue(..., 0, ...)` yields the non-empty section value
`"00"`, `getDateFromDateSections` parses `"00 07 2026"` → `Invalid Date`, and the branch
above fires.

### E3 — Reproduced end to end in Vitest + jsdom (191 ms, no backend)

A throwaway spec rendering the **real, unmocked** `DateRangeSelector`:

```
RCA onStart calls: [[null]]
RCA arg: Invalid Date | getTime=NaN
```

Note the first line: `JSON.stringify` renders an `Invalid Date` as `null`. Any developer
who logged the callback argument would have concluded MUI passes `null` and that the
existing guard is sufficient. It does not, and it is not.

A stateful harness reproducing `BaseMetricsView`'s guard and `DashboardHeader`'s label
verbatim:

```
RCA harness commits (1): INVALID
RCA harness threw: RangeError: Invalid time value
```

**One keystroke.** The user cannot even reach the second character of `05.07.2026`.

### E4 — Every keystroke is committed; `onAccept` is not a "done" signal

Typing the eight characters `05072026` into a `dd.MM.yyyy` field:

```
PROBE onChange: 8  INVALID -> 2026-7-5 -> INVALID -> 2026-7-5 -> 2-7-5 -> 20-7-5 -> 202-7-5 -> 2026-7-5
PROBE onAccept: 8  INVALID -> 2026-7-5 -> INVALID -> 2026-7-5 -> 2-7-5 -> 20-7-5 -> 202-7-5 -> 2026-7-5
PROBE onBlur:   1
```

Three findings:
- 8 commits for one date entry → 8 state updates, 8 URL rewrites, 8 metrics refetch waves.
- 3 of them (`0002-07-05`, `0020-07-05`, `0202-07-05`) are *valid* `Date` objects and would
  each ask the backend for the year 2. A validity check alone does not stop them.
- **`onAccept` fires identically to `onChange`** while typing. The obvious fix — "just move
  to `onAccept`" — does nothing. Only `onBlur` is a genuine once-per-edit signal.

### E5 — The reporter's error screen

`main.tsx:200` wraps the app in `AppErrorBoundary`
(`components/Common/ErrorBoundary/AppErrorBoundary.tsx:28-32`), which renders the thrown
`error.message` in a full-viewport MUI `Alert`. That is the screenshot on the bug.

---

## 3. Five Whys — Multi-Causal Chains

### Branch A — the crash

> **WHY 1A — The Metrics view throws and the app is replaced by the error boundary.**
> Evidence: E3 — `RCA harness threw: RangeError: Invalid time value` after a single `0`
> keystroke; E5 — `main.tsx:200` renders the boundary fallback for any render-time throw.

> > **WHY 2A — Because `DashboardHeader` formats the start/end date unconditionally with date-fns.**
> > Evidence: `pages/Common/MetricsView/DashboardHeader.tsx:50`
> > ```ts
> > const formatDate = (d: Date) => format(d, "dd MMM yyyy");
> > ```
> > called at `:118` — `{` ${formatDate(startDate)} → ${formatDate(endDate)}`}`. E1 proves
> > date-fns 4.4.0 `format()` throws `RangeError: Invalid time value` on an invalid date.
> > (Two further render-path throw sites exist behind the same input:
> > `pages/Common/MetricsView/usePercentilesOverTime.ts:23` and
> > `pages/Common/MetricsView/usePbcOverTime.ts:24`, both building a cache key with
> > `startDate.toISOString()` *during render*, at line `:49`/`:51` respectively.)

> > > **WHY 3A — Because `BaseMetricsView` stored an `Invalid Date` in its `startDate` state.**
> > > Evidence: `pages/Common/MetricsView/BaseMetricsView.tsx:1224-1228`
> > > ```ts
> > > const handleStartDateChange = (date: Date | null) => {
> > > 	if (!date) return;
> > > 	setStartDate(date);
> > > 	updateDateParams(date, endDate);
> > > };
> > > ```
> > > The guard tests falsiness only. E1 proves `!invalidDate === false`, so an
> > > `Invalid Date` sails straight into `setStartDate`.

> > > > **WHY 4A — Because `DateRangeSelector` forwards MUI's raw payload with no validation, and its type signature says that is safe.**
> > > > Evidence: `components/Common/DateRangeSelector/DateRangeSelector.tsx:73`
> > > > ```tsx
> > > > onChange={(newValue) => onStartDateChange(newValue as Date | null)}
> > > > ```
> > > > (identically at `:108` for the end date). The prop is typed
> > > > `onStartDateChange: (date: Date | null) => void` at `:37`. TypeScript cannot
> > > > distinguish `Date` from `Invalid Date` — both are `Date` — so the declared contract
> > > > is `Date | null` while the **actual** contract is `Date | InvalidDate | null`. The
> > > > `as Date | null` cast at the call site actively asserts the narrower, false version.
> > > > Every downstream `if (!date)` guard is written against the type, and is therefore
> > > > correct against the type and wrong against reality.

> > > > > **WHY 5A — Because nothing in the codebase owns the job of validating what the date picker emits, and the one place that could have caught it was written to fail silently.**
> > > > > Evidence: there is no `isValidDate` helper anywhere in
> > > > > `Lighthouse.Frontend/src/utils/date/` (`age.ts`, `blockedDuration.ts`,
> > > > > `formatDuration.ts`, `localDate.ts` — none provides one). The single shared
> > > > > date-encoding boundary, `utils/date/localDate.ts:13-18`, is *aware* of malformed
> > > > > input in the read direction — `parseLocalDate` at `:29-34` explicitly rejects
> > > > > overflow dates ("Rejects overflow dates such as 2026-07-99") — but the write
> > > > > direction has no equivalent:
> > > > > ```ts
> > > > > export function formatLocalDate(date: Date): string {
> > > > > 	const year = date.getFullYear();
> > > > > 	...
> > > > > ```
> > > > > which E1 shows returns `"NaN-NaN-NaN"` rather than failing. The asymmetry is the
> > > > > root: input from the *URL* is validated, input from the *picker* is trusted.
> > > > >
> > > > > **→ ROOT CAUSE A: The application has no validated boundary for date-picker
> > > > > output. `DateRangeSelector` is a pass-through whose declared type asserts a
> > > > > guarantee MUI-X does not make, and the shared date encoder silently accepts `NaN`
> > > > > instead of refusing it.**

### Branch B — no fallback to the last working date

> **WHY 1B — When the entry goes bad, the previous working date is gone.**
> Evidence: E3 — the harness's `commits` array contains exactly `["INVALID"]`; the prior
> value `2026-07-15` has been overwritten in state and is not retained anywhere.

> > **WHY 2B — Because the committed value and the in-progress edit are the same variable.**
> > Evidence: `BaseMetricsView.tsx:1207-1215` holds `startDate`/`endDate` in `useState`;
> > `DateRangeSelector.tsx:70,105` passes them straight back down as the controlled
> > `value` of each `DatePicker`. There is no draft, no `lastValid` ref, no second slot.

> > > **WHY 3B — Because the design treats every emission as a commit, so there is no state in which "the user is mid-edit" can be represented.**
> > > Evidence: E4 — 8 `onChange` emissions produce 8 commits. `DateRangeSelector` holds
> > > no state at all: `:44-49` destructures props and `:51` computes `localDateFormat`;
> > > there is no `useState` in the file.

> > > > **WHY 4B — Because recovery was never designed, since an unrecoverable input was assumed impossible.**
> > > > Evidence: the only defensive logic in the whole chain is the null check at
> > > > `BaseMetricsView.tsx:1225` / `:1231`. Its presence proves the author considered
> > > > "the picker might hand back nothing" and handled it; its form proves they believed
> > > > `null` was the *only* degenerate case MUI could produce. E2 shows MUI-X documents
> > > > the opposite in its own source comment.

> > > > > **WHY 5B — Same fundamental cause as A: with no validated boundary there is nothing to reject, and with nothing to reject there is nothing to fall back *to*.**
> > > > > Evidence: the two behaviours are the same missing line of code. A guard that
> > > > > refuses an invalid date *is* the fallback — the state simply keeps its previous
> > > > > value. Confirmed by the harness: had `handleStartDateChange` returned early on
> > > > > `Number.isNaN(date.getTime())`, `startDate` would still hold `2026-07-15` and
> > > > > `format()` at `DashboardHeader.tsx:50` would never have been reached.
> > > > >
> > > > > **→ ROOT CAUSE A (shared). (b) is not a separate defect; it is the same defect
> > > > > observed from the recovery side.**

### Branch C — commit on every keystroke

> **WHY 1C — Editing one date fires many state updates and many metrics reloads.**
> Evidence: E4 — `PROBE onChange: 8` for the eight characters of `05072026`; three of the
> eight are the nonsense-but-valid years `0002`, `0020`, `0202`.

> > **WHY 2C — Because `DateRangeSelector` forwards `onChange`, and `onChange` is a keystroke event, not a completion event.**
> > Evidence: `DateRangeSelector.tsx:73` and `:108` wire the parent callback directly to
> > `DatePicker`'s `onChange`. E2 shows `publishValue` runs on each section update.

> > > **WHY 3C — Because each commit rewrites the URL, which resets the fetch gate and refetches everything visited.**
> > > Evidence: `BaseMetricsView.tsx:1217-1222` — `updateDateParams` writes both params on
> > > every change; `:1249-1252` uses the date pair as the **reset token** for
> > > `useVisitedCategories`:
> > > ```ts
> > > const visitedCategories = useVisitedCategories(
> > > 	selectedCategory,
> > > 	`${entity.id}:${formatLocalDate(startDate)}:${formatLocalDate(endDate)}`,
> > > );
> > > ```
> > > and `:1295-1301` feeds `startDate`/`endDate` into `useMetricsData`. So one keystroke
> > > = one gate reset = one full refetch of every visited category. This is Bug #5571's
> > > machinery working exactly as designed, driven eight times by one date entry.

> > > > **WHY 4C — Because no edit-completion signal was ever looked for, and the obvious candidate does not work.**
> > > > Evidence: `DateRangeSelector.tsx` passes neither `onAccept` nor a `textField.onBlur`
> > > > through `slotProps` (`:78-92`, `:113-127` set only `size`, `fullWidth` and day
> > > > styling). E4 proves this was not merely an omission of the easy fix: `onAccept`
> > > > fires 8 times, identically to `onChange`, because
> > > > `internals/hooks/usePicker/hooks/useValueAndOpenStates.js:112` defaults
> > > > `changeImportance = 'accept'` for field-originated updates. Only `onBlur` fires
> > > > once.

> > > > > **WHY 5C — Because the component was designed for calendar-click selection, where one interaction is one complete date, and keyboard section-editing was never modelled.**
> > > > > Evidence: the styling investment is entirely in the calendar surface —
> > > > > `DateRangeSelector.tsx:83-90` and `:118-125` style `slotProps.day` and
> > > > > `.Mui-selected`; nothing styles or handles the text field beyond `size`/`fullWidth`.
> > > > > The same assumption is written down on the E2E side, at
> > > > > `Lighthouse.EndToEndTests/tests/models/metrics/MetricsPage.ts:339-346`:
> > > > > ```
> > > > > * them back on every picker change), so a range is applied by deep-linking and
> > > > > * reloading rather than by typing into the MUI date fields — the fields are
> > > > > * locale-formatted section inputs, which would make the spec depend on the
> > > > > * runner's locale.
> > > > > ```
> > > > > The phrase "on every picker change" records the per-keystroke commit as
> > > > > *intended* behaviour, and the decision not to type is stated outright.
> > > > >
> > > > > **→ ROOT CAUSE C: The date range has no commit boundary. `DateRangeSelector`
> > > > > treats every `onChange` emission as a finished user decision because it was
> > > > > designed around calendar clicks, where that happens to be true; for keyboard
> > > > > section-editing it is false on every character but the last.**

### Branch D — silent corruption (not reported, discovered during investigation)

> **WHY 1D — An invalid date does not only crash; on the paths that do not throw, it corrupts data silently.**
> Evidence: E1 — `formatLocalDate(invalid) -> NaN-NaN-NaN`, no throw.

> > **WHY 2D — Because that string is written to the browser URL and to every metrics request.**
> > Evidence: `BaseMetricsView.tsx:1219-1220` writes it into `startDate`/`endDate` search
> > params; `services/Api/MetricsService.ts:887-894`:
> > ```ts
> > getDateFormatString(startDate: Date, endDate: Date): string {
> > 	const formattedStartDate = formatLocalDate(startDate);
> > 	const formattedEndDate = formatLocalDate(endDate);
> > 	return `startDate=${formattedStartDate}&endDate=${formattedEndDate}`;
> > }
> > ```
> > A `startDate=NaN-NaN-NaN` query would reach the backend on any code path where the
> > request beats the render throw.

> > > **WHY 3D — Because `formatLocalDate` is the single shared encoder for both sinks, so one unguarded write corrupts both at once.**
> > > Evidence: the comment at `MetricsService.ts:888-889` — "Shared with the dashboards'
> > > URL params on purpose: the two encodings disagreeing is what shifted shared links by
> > > a day (Bug #5566)." The sharing is deliberate and correct; it also means the missing
> > > guard has twice the blast radius.

> > > > **WHY 4D — Because the reverse direction was hardened after Bug #5566 and the forward direction was not.**
> > > > Evidence: `utils/date/localDate.ts:29-34` validates the round trip on parse
> > > > (`isTheDayItClaimsToBe`); `:13-18` performs no check on format. The file's own
> > > > header comment (`:1-9`) reasons carefully about UTC-vs-local correctness and not at
> > > > all about validity.

> > > > > **WHY 5D — Same as A: no validated boundary. Here the absence expresses itself as silent corruption rather than a throw, which is strictly worse — an `Invalid Date` reaching only this path would produce a wrong dashboard with no error at all.**
> > > > > Evidence: E1 shows `format()`/`toISOString()` throw while `formatLocalDate`
> > > > > returns a string. Whether the user sees a crash or a silently wrong dashboard is
> > > > > decided purely by which sink renders first.
> > > > >
> > > > > **→ ROOT CAUSE A (shared), plus a distinct hardening gap in `formatLocalDate`.**

### Branch E — why this shipped (cross-cutting)

> **WHY 1E — A one-keystroke, 100 %-reproducible crash on a core screen reached production.**
> Evidence: E3 reproduces in 191 ms in jsdom on unmodified `main`.

> > **WHY 2E — Because no test in either suite ever types into a real MUI date field.**
> > Evidence: three layers of mocking, each replacing the component whose contract is the
> > defect.
> > - `components/Common/DateRangeSelector/DateRangeSelector.test.tsx:23-46` mocks
> >   `@mui/x-date-pickers` wholesale; the stub is a `<button>` whose click calls
> >   `onChange(new Date(2023, 1, 15))` — always valid, never typed.
> > - `pages/Common/MetricsView/DashboardHeader.test.tsx:5-29` mocks `DateRangeSelector`
> >   with buttons emitting `new Date("2020-01-01")` / `new Date("2020-01-02")`.
> > - `pages/Common/MetricsView/BaseMetricsView.test.tsx:461-532` mocks `DashboardHeader`
> >   with buttons emitting `startDate - 30 days` and a fixed `new Date(2026, 5, 15)`.
> >
> > Not one of the ~7 000 lines of test across these four files ever produces an invalid
> > date, because at every level the thing that produces invalid dates has been replaced
> > by a stub that cannot.

> > > **WHY 3E — Because the E2E suite deliberately does not type either, and says so.**
> > > Evidence: `MetricsPage.ts:339-346` (quoted in WHY 5C) — the range is applied by
> > > deep-linking and page reload. `MetricsDateRange.applyAndWaitFor` at `:388-407` calls
> > > `page.goto(url)`; the only date-field interaction in the whole POM is
> > > `getByTestId("dashboard-date-range-toggle")` at `:350-352`, which opens the popover
> > > and stops there. Only three specs use it at all: `PredictabilityOverTime.spec.ts`,
> > > `WorkItemAgeAsOfRangeEnd.spec.ts`, and the POM itself.

> > > > **WHY 4E — Because the mocking was motivated by real, reasonable problems, and nobody re-examined the cost.**
> > > > Evidence: `DateRangeSelector.test.tsx:48-49` — "Mock the implementation of
> > > > getLocaleDateFormat to avoid `Intl.DateTimeFormat` issues"; the E2E comment cites
> > > > runner-locale dependence. Both are genuine. Both were solved by removing the
> > > > component instead of pinning the locale — even though the production component
> > > > already carries `_testLocalDateFormat` (`DateRangeSelector.tsx:39,47,51`) for
> > > > exactly that purpose, which would have made a real-picker test locale-independent.
> > > > The escape hatch existed and was not used.

> > > > > **WHY 5E — Because the test strategy mocks at the boundary where the risk actually lives, and that choice was never revisited. Third-party integration points are precisely where a declared contract and real behaviour can diverge; stubbing them is convenient at every individual call site and leaves the seam uncovered at all of them. This is not a defensible trade-off that happened to lose — it is a gap nobody was looking at.**
> > > > > Evidence: the defect is *entirely* in the seam between MUI-X's emission (E2) and
> > > > > the app's handler; there is no bug in any line the tests do cover. `DateRangeSelector`
> > > > > contains 133 lines, of which the 2 that matter (`:73`, `:108`) are the 2 the mock
> > > > > replaces.
> > > > >
> > > > > **→ ROOT CAUSE E: Every test layer mocks away the third-party component whose
> > > > > real contract is the defect, so the integration seam has zero coverage at any
> > > > > level. This is why A and C shipped and stayed shipped.**

---

## 4. Cross-Validation

**Backwards chain (root cause → symptom), each independently verified:**

| Chain | Forward trace | Verified by |
|---|---|---|
| A → (a) | No validation at `DateRangeSelector.tsx:73` ⇒ `Invalid Date` passes `BaseMetricsView.tsx:1225` ⇒ stored in state ⇒ `format()` at `DashboardHeader.tsx:50` throws ⇒ `AppErrorBoundary` | E3 harness: 1 commit `INVALID`, then `RangeError: Invalid time value` |
| A → (b) | No validation ⇒ nothing is rejected ⇒ the previous value is overwritten rather than kept | E3: `commits` = `["INVALID"]`; prior `2026-07-15` gone |
| A → (d) | No validation ⇒ `formatLocalDate` emits `NaN-NaN-NaN` ⇒ URL params and `getDateFormatString` carry it | E1 + `BaseMetricsView.tsx:1219`, `MetricsService.ts:890-893` |
| C → (c) | `onChange` wired straight to the parent ⇒ 8 commits per date ⇒ 8 URL writes ⇒ 8 gate resets at `BaseMetricsView.tsx:1251` ⇒ 8 refetch waves | E4: `PROBE onChange: 8`, `PROBE onBlur: 1` |
| E → A, C persisting | All four test files stub the emitting component ⇒ no test can produce an invalid or intermediate date | `DateRangeSelector.test.tsx:23`, `DashboardHeader.test.tsx:5`, `BaseMetricsView.test.tsx:461`, `MetricsPage.ts:339` |

**Consistency.** A, C and E do not contradict each other. A and C are independent
mechanisms in the same component (`:73`) and are separately falsifiable: fixing only A
still leaves 8 commits per entry (E4 shows 6 of the 8 emissions are valid dates that a
validity guard would happily commit); fixing only C still leaves the crash reachable via
the final keystroke of an out-of-range entry such as `31.02.2026`. E is a meta-cause
explaining persistence, not occurrence.

**Completeness.** All reported symptoms are accounted for, plus one the reporter could not
have seen (Branch D). No symptom lacks a chain; no chain lacks evidence.

**Status of Branch D — deliberately not a fourth root cause.** D shares Root Cause A: it is
the same missing validation, observed on the one sink that fails silently instead of
throwing. It is written as its own branch because it has a *separate fix* (hardening
`formatLocalDate`) and a *worse failure mode* (a wrong dashboard with no error), not because
it is independently rooted. Removing Root Cause A removes D entirely; the P1 hardening is
defence in depth on top.

**Standing of E1 vs E2.** E1 — the probe run against this repo's own `node_modules` — is
the ground truth for every behavioural claim (`Invalid Date` is truthy; `format` and
`toISOString` throw; `formatLocalDate` does not). E2, the MUI-X source comment, corroborates
*why* the library behaves that way but is not independent verification of *that* it does.
The independent verification is E3/E4, which observe the real component's actual emissions.
If E2's comment were stale, the analysis would stand unchanged.

**Answer to "do (a), (b), (c) share one root cause?"**

- **(a) and (b) share ROOT CAUSE A.** They are one defect seen from two sides: the missing
  validation is simultaneously the missing crash-guard and the missing fallback. Fixing (a)
  correctly yields (b) for free — a rejected value leaves the previous state untouched.
- **(c) is ROOT CAUSE C, genuinely separate.** Proof from E4: after removing every invalid
  emission, six valid ones remain (`2026-7-5`, `2026-7-5`, `2-7-5`, `20-7-5`, `202-7-5`,
  `2026-7-5`), each of which a validity guard would commit and each of which triggers a
  refetch. A validity fix alone does not satisfy the reporter's third request.
- **ROOT CAUSE E is why all three shipped** and must be addressed or the class recurs.

**Rejected hypothesis.** "MUI-X passes `null` and the guard is adequate." Falsified by E3:
`JSON.stringify(onStart.mock.calls)` prints `[[null]]` — a serialisation artefact — while
the live argument is `Invalid Date` with `getTime() === NaN`. Recorded because this is the
trap that would mislead the next person to debug it from console output.

---

## 5. Proposed Fix

Not implemented — investigation only. Ordered by priority.

**Ordering constraint:** P0 must ship before the `formatLocalDate` change in P1. That change
turns a silent `NaN-NaN-NaN` into a throw, so landing it first would move the crash rather
than remove it. Everything else is order-independent.

### P0 — `DateRangeSelector.tsx` becomes the validated commit boundary

**File:** `Lighthouse.Frontend/src/components/Common/DateRangeSelector/DateRangeSelector.tsx`

Addresses Root Causes A **and** C together, at the one place both live.

1. Introduce draft state, seeded from props and resynced when props change:
   `const [draftStart, setDraftStart] = useState(startDate)` (likewise `draftEnd`), with
   an effect resyncing on prop change so external updates (deep link, reset) still land.
2. Rewire `:73` and `:108` so `onChange` updates the **draft only** — never the parent.
   The field stays fully responsive; nothing downstream moves.
3. Commit on a real completion signal. Per E4 the only once-per-edit signal is blur, so
   pass `slotProps.textField.onBlur`; also commit from the calendar's `onAccept`, which for
   a mouse selection is genuinely one interaction.
4. Commit only through a guard: valid **and** within `[minDate, maxDate]`, **and** different
   from the current committed value. On failure, reset the draft to the prop — that is the
   reporter's "reset to the previously working date", and it is one line.
5. **Flush on unmount.** `DashboardHeader.tsx:126-142` renders this component inside a
   `<Popover>`; closing the popover unmounts it, and React does not reliably fire `blur` on
   unmount. Without a `useEffect` cleanup that flushes a pending valid draft, a user who
   types a date and clicks the backdrop loses the edit. This is the single most likely way
   to get the fix wrong.

### P0 — `isValidDate` helper

**New file:** `Lighthouse.Frontend/src/utils/date/isValidDate.ts`

```ts
export function isValidDate(value: unknown): value is Date {
	return value instanceof Date && !Number.isNaN(value.getTime());
}
```

One named predicate so the check reads the same everywhere and cannot be written three
subtly different ways. It sits beside `localDate.ts`, the file that already owns the parse
side of the same question.

### P1 — `BaseMetricsView.tsx` defence in depth

**File:** `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx:1224-1234`

Replace `if (!date) return;` with `if (!isValidDate(date)) return;` in both handlers. Even
with P0 in place, this is the last line before state; it costs nothing and it means no
future caller of these handlers can reintroduce the crash.

### P1 — render-path `toISOString` cache keys

**Files:**
`Lighthouse.Frontend/src/pages/Common/MetricsView/usePercentilesOverTime.ts:23`
`Lighthouse.Frontend/src/pages/Common/MetricsView/usePbcOverTime.ts:24`

Both build a cache key with `startDate.toISOString()` **during render** (`:49` and `:51`),
so both throw on an invalid date exactly as `DashboardHeader` does. Switch to
`formatLocalDate` from `utils/date/localDate`. This is not only hardening: keying a
*local* calendar window by its UTC instant is the same latent mismatch Bug #5566 fixed
everywhere else in this dashboard, and these two files are the stragglers. It also makes
the key stable across a UTC day boundary, which is what the surrounding code already
assumes.

### P1 — `formatLocalDate` stops emitting `NaN-NaN-NaN`

**File:** `Lighthouse.Frontend/src/utils/date/localDate.ts:13-18`

Make the encoder refuse an invalid date rather than emit `"NaN-NaN-NaN"` into the URL and
the query string (Branch D). Throwing turns silent corruption into a loud, test-visible
failure and matches the rigour `parseLocalDate` already applies at `:29-34`.

**Trade-off to decide before implementing:** throwing here converts Branch D's silent
corruption into a *new* crash site if any upstream guard is ever missed. Given P0 and the
`BaseMetricsView` guard both sit upstream, and given a `NaN-NaN-NaN` request is a
user-visible wrong answer with no error, the loud failure is the better default — but it
must land *after* P0, not before.

### P2 — `DashboardHeader.tsx` label

**File:** `Lighthouse.Frontend/src/pages/Common/MetricsView/DashboardHeader.tsx:50`

Have `formatDate` return a placeholder for an invalid date instead of throwing. Belt and
braces: with P0 and P1 in place nothing invalid can reach it — `parseLocalDate` already
rejects a malformed URL param and falls back to the default window
(`BaseMetricsView.tsx:1207-1215`) — but a label is never worth an application crash.

### P0 — stop mocking the picker: the regression test *is* the fix for Root Cause E

**File:** `Lighthouse.Frontend/src/components/Common/DateRangeSelector/DateRangeSelector.test.tsx`

Root Cause E is a defect in its own right, not a lesson learned, and it gets a P0 like the
others. The fix is a describe block that renders `DateRangeSelector` with
`@mui/x-date-pickers` **unmocked** and types into the field, pinning the format via
`_testLocalDateFormat`. Cases and rationale are in §8.

Without it, P0 and P1 are unverifiable by the suite and the next person to simplify
`DateRangeSelector` reintroduces the bug against a green build — which is exactly the
history that produced this ticket. Owner: whoever implements the P0 component change, in
the same commit.

### P2 — `DatePickerComponent.tsx` is dead code carrying the same latent bug

**File:** `Lighthouse.Frontend/src/components/Common/DatePicker/DatePickerComponent.tsx`

Nothing imports it except its own test — verified by grep across `src`; the sole hit is
`DatePickerComponent.test.tsx:5`. Its guard at `:19` is
`if (!newValue || newValue.isAfter(dayjs())) return;`, and E1 proves
`dayjs(invalid).isAfter(dayjs())` is `false`, so an invalid `Dayjs` passes straight
through — the identical defect in the dayjs dialect. Delete the component and its test, or
if it is being kept for a planned use, change the guard to `!newValue?.isValid()`. Leaving
it as-is means the bug gets reintroduced the day someone reaches for it.

---

## 6. Files Affected

**Production code (to change):**
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/components/Common/DateRangeSelector/DateRangeSelector.tsx` — P0, the primary fix
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/utils/date/isValidDate.ts` — P0, new file
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx` — P1, lines 1224-1234
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/pages/Common/MetricsView/usePercentilesOverTime.ts` — P1, line 23
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/pages/Common/MetricsView/usePbcOverTime.ts` — P1, line 24
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/utils/date/localDate.ts` — P1, lines 13-18
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/pages/Common/MetricsView/DashboardHeader.tsx` — P2, line 50
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/components/Common/DatePicker/DatePickerComponent.tsx` — P2, delete or fix line 19

**Tests requiring change:**
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/components/Common/DateRangeSelector/DateRangeSelector.test.tsx` — **will break**, see risk R1
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/components/Common/DatePicker/DatePickerComponent.test.tsx` — delete alongside its component

**Evidence-only, no change expected:**
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/pages/Common/MetricsView/DashboardHeader.test.tsx`
- `/storage/repos/Lighthouse/Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.test.tsx`
- `/storage/repos/Lighthouse/Lighthouse.EndToEndTests/tests/models/metrics/MetricsPage.ts`

---

## 7. Risk Assessment

### R1 — `DateRangeSelector.test.tsx` will fail (HIGH likelihood, LOW severity)

Two tests — "calls onStartDateChange when start date changes" (`:91-107`) and its end-date
twin (`:109-121`) — click a mocked `DatePicker` stub that calls `onChange(...)` and assert
the parent callback fired. Under the P0 design `onChange` updates the draft only, so both
go red. They are **not** regressions; they encode the very behaviour being removed. They
must be rewritten against the new contract (commit on blur). Anyone treating them as a
regression and "fixing" the component to keep them green reinstates the bug.

### R2 — Lost edit on popover close (MEDIUM likelihood, HIGH severity)

The unmount-flush described in P0 step 5. `DashboardHeader.tsx:126-142` mounts the selector
inside a `<Popover>` with `disableEnforceFocus` and `disablePortal`; the popover unmounts
its children on close, and blur-on-unmount is not guaranteed. The failure mode — user types
a date, clicks away, dashboard silently ignores it — is worse than the bug being fixed
because it is silent. This is the acceptance criterion the fix most needs.

### R3 — Behaviour change in URL-param write frequency (LOW likelihood, MEDIUM severity)

Today the `startDate`/`endDate` params are rewritten on every keystroke. After the fix they
change once per completed edit. Anything observing param churn rather than param value
would notice. Surveyed: `useVisitedCategories` (`BaseMetricsView.tsx:1249-1252`) keys off
the formatted values, not the write count, and `useMetricsData` keys off the `Date` objects;
neither cares. Assessed low, but it is the kind of coupling that hides.

### R4 — Fewer, later fetches could unmask a race (LOW likelihood, MEDIUM severity)

Collapsing 8 refetch waves to 1 removes the accidental retry cover that overlapping
in-flight requests currently provide. Bug #5571's gate is monotonic within an `(entity,
window)` pair (`useMetricsData.ts:209-214`), so the reduction is safe by construction — but
if any widget's fetch were quietly relying on being re-issued, it would surface now.

### R5 — MUI-X upgrade sensitivity (LOW likelihood, LOW severity)

A regression test that types into the real `@mui/x-date-pickers` field couples to that
library's DOM. Concretely: sections expose `role="spinbutton"` and are addressed by
`aria-label` (`"Day"`, `"Month"`, `"Year"`) — `getAllByRole("textbox")` finds nothing,
which cost one iteration during this investigation. A major upgrade could rename them.
This is the correct coupling to accept: the whole point is to test the real contract, and a
test that breaks on a picker upgrade is a test doing its job. Note it in the spec so the
next person is not confused.

### Existing test coverage touching these components

**Vitest / React Testing Library:**

| File | Lines | Relationship |
|---|---|---|
| `src/components/Common/DateRangeSelector/DateRangeSelector.test.tsx` | 149 | Mocks `@mui/x-date-pickers` at `:23`. **Will break.** |
| `src/pages/Common/MetricsView/DashboardHeader.test.tsx` | 238 | Mocks `DateRangeSelector` at `:5`. Unaffected. |
| `src/pages/Common/MetricsView/BaseMetricsView.test.tsx` | 6 266 | Mocks `DashboardHeader` at `:461`; exercises the handlers directly (`:1244-1300`), incl. the Bug #5566 local-midnight cases at `:517-528`. Unaffected by P0; the P1 guard change must keep these green. |
| `src/components/Common/DatePicker/DatePickerComponent.test.tsx` | 57 | Tests the dead component. Delete with it. |
| `src/pages/Common/MetricsView/usePbcOverTime.test.ts` | 148 | Covers the cache key; must be checked against the `formatLocalDate` switch. |
| `src/pages/Common/MetricsView/usePercentilesOverTime.test.ts` | 166 | Same. |
| `src/utils/date/localDate.test.ts` | — | Owns `formatLocalDate`; extend for the invalid-input contract. |

**Playwright E2E** — three specs use `MetricsDateRange`, all via deep-link + reload, none
typing, so none is at risk from P0:
`Lighthouse.EndToEndTests/tests/specs/flow/PredictabilityOverTime.spec.ts`,
`.../flow/WorkItemAgeAsOfRangeEnd.spec.ts`, and the POM
`.../tests/models/metrics/MetricsPage.ts`.

Fourteen further specs render the metrics dashboard without touching the range —
`csv.spec.ts`, `flow/AgingPacePercentiles`, `flow/BlockedItems`, `flow/CumulativeStateTime`,
`flow/FlowEfficiency`, `flow/NamedCycleTimePercentiles`, `flow/PbcOverTime`,
`flow/PercentilesOverTime`, `flow/TimeInStateAndStaleness`, `flow/TotalThroughputViewData`,
`flow/WorkItemAgePercentilesStatus`, `metrics/MultipleCycleTimes`,
`portfolios/DeliveryMetrics`, `screenshots/Screenshots` — none of which types into a date
field either.

---

## 8. Regression Test: Vitest Component Level

**Recommendation: Vitest + React Testing Library, on `DateRangeSelector`, with
`@mui/x-date-pickers` NOT mocked.**

**Why this level:**

1. **The defect lives exactly where the mocks are.** The bug is entirely in the seam
   between MUI-X's `publishValue` (E2) and the app's handler. A test that mocks the picker
   cannot see it — that is not a hypothesis, it is the demonstrated history: four test
   files, ~7 000 lines, all three layers stubbed, zero detection. A regression test that
   keeps the mock would pass today, before any fix.
2. **It reproduces deterministically and fast.** Measured at 191 ms in jsdom, no backend,
   no server, no demo data: `getAllByLabelText("Day")` → `user.keyboard("0")` → assert the
   parent was not called with an invalid date and that the last valid date survives.
3. **Locale dependence — the reason E2E declined to type — is already solved here.** The
   production component accepts `_testLocalDateFormat` (`DateRangeSelector.tsx:39,47,51`).
   Pinning `"dd.MM.yyyy"` makes the test independent of the runner's `Intl` locale, which
   is precisely what `MetricsPage.ts:343-345` could not do from the browser.
4. **All three sub-problems are assertable at this level:** (a) the parent is never called
   with an invalid date; (b) after an invalid keystroke the committed value still equals
   the previous one; (c) typing `05072026` produces exactly **one** parent call — the
   number is the assertion, and E4 gives 8 as the pre-fix baseline.
5. **It matches project convention.** E2E here is a thin walking-skeleton sanity check
   driven from demo data through Page Objects; a keystroke-level input-validation case is
   not a walking skeleton, and adding it would cost roughly a hundred times as much per run
   for strictly less assertion power.

**Suggested cases** (all in
`src/components/Common/DateRangeSelector/DateRangeSelector.test.tsx`, in a new describe
block that does **not** mock the picker):

- typing `0` into the day section never calls `onStartDateChange` with an invalid date
- typing `0` into the month section, likewise
- after the invalid keystroke, the committed start date still equals the initial prop
- typing the full `05072026` then blurring calls `onStartDateChange` exactly once, with
  2026-07-05
- an out-of-range completed date (e.g. `31022026`) is rejected and the previous value stands
- closing the popover after a valid entry still commits it (guards R2 — this one belongs in
  `DashboardHeader.test.tsx`, with the real `DateRangeSelector` mounted)

**One E2E case is worth considering, but only one:** a single spec asserting the app does
not show the error boundary after typing into the metrics date field. It buys the
white-screen guarantee end to end. It also re-imports the locale coupling that
`MetricsPage.ts:339-346` deliberately avoided, so it must pin the browser locale in the
Playwright project config. Recommended as optional; the Vitest suite above is the actual
regression barrier.

---

## 9. Prevention

| # | Action | Addresses | Priority |
|---|---|---|---|
| P-1 | Adopt the rule: **a third-party input component may be mocked in tests of its consumers, but must have at least one test that renders it for real.** The seam is where contracts diverge; stub it everywhere and it has no coverage anywhere. | Root Cause E | P1 |
| P-2 | Add `isValidDate` and require it wherever a `Date` crosses a component or module boundary from a picker, a URL or an API. `Date | null` does not mean what its readers think. | Root Cause A | P1 |
| P-3 | Audit every remaining render-path `toISOString()` / date-fns `format()` on a user-supplied date. Both throw; a throw during render unmounts the app. The two known stragglers are listed in §5. | Branch A, D | P2 |
| P-4 | Record in `docs/ci-learnings.md` that MUI-X `onChange` **and `onAccept`** both fire per keystroke while a field is being typed, that `onAccept` is therefore not a completion signal, and that `JSON.stringify(invalidDate)` prints `null` — the trap that makes this class of bug look like a null-handling bug. | Root Cause C, E | P2 |
| P-5 | Delete `DatePickerComponent.tsx`. Dead code that carries a live bug is worse than dead code. | Branch A (latent) | P2 |

---

## 10. Peer Review Log

Reviewed by `nw-troubleshooter-reviewer` on 2026-09-04 against commit `853a31d21`.

**Accepted and applied:**
- Root Cause E had no priority-labelled fix, only a recommendation → added as a P0 in §5,
  owned by the same commit as the component change.
- The P0-before-P1 ordering constraint was buried in the P1 prose → hoisted to the §5 header.
- Branch D's standing (variant of A vs. independent root) was ambiguous → stated explicitly
  in §4.
- E1 (probe) vs E2 (MUI-X source comment) evidentiary standing was not distinguished →
  stated explicitly in §4.
- WHY 5E could be read as defending the mocking choice → reworded to name it as an unexamined
  gap.

**Rejected, with evidence:**
- *"Line numbers in `DateRangeSelector.tsx`, `usePercentilesOverTime.ts` and `usePbcOverTime.ts`
  are off by 1-4 lines"* (raised MEDIUM). **False.** Re-verified against the working tree with
  absolute line numbering:
  - `DateRangeSelector.tsx:37` → `onStartDateChange: (date: Date | null) => void;` ✓
  - `usePercentilesOverTime.ts:23` → `return \`${selection}|${startDate.toISOString()}|${endDate.toISOString()}\`;` ✓
  - `usePbcOverTime.ts:24` → `return \`${metricType}|${startDate.toISOString()}|${endDate.toISOString()}\`;` ✓
  - `AppErrorBoundary.tsx:28-32` → `getDerivedStateFromError` returning `error.message` ✓
  - `main.tsx:200` → `<AppErrorBoundary>` ✓

  Every citation in this document is absolute-file-line and correct. The reviewer's reader
  reported symbol-relative offsets. Worth knowing: a reviewer reading through a
  symbol-anchored view will produce false line-drift findings on any document that cites
  absolute lines.
