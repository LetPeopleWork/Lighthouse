# Mutation testing — epic 4172 (Forecast Reality Check)

One section per story, oldest first. Every story gates on an 80 % kill rate.

## Story 6072 (Forecast reality check, backend)

Run 2026-09-26 against `main` @ `5e50547e5`, plus the tests this pass added. Gate is 80 % kill rate.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | **97.53 %** | 81 | 79 | 2 | 0 | 3 m 21 s |
| Frontend | N/A | — | — | — | — | — |

Config: `stryker.6072.backend.json`. Runs from `Lighthouse.Backend.Tests/`.

A first backend run on `5e50547e5` scored **91.36 %** with 7 survivors (81 tested, 74 killed, 3 m 21 s).
Five of them were missing tests and are closed below; the two left are equivalent.

### Backend

| file | tested | survived | first run |
| --- | --- | --- | --- |
| RealityCheckVerdictPolicy.cs | 59 | 0 | 59 / 2 |
| ForecastRealityCheckService.cs | 21 | 2 | 21 / 4 |
| ThroughputFilterOverride.cs | 1 | 0 | 1 / 1 |

#### Closed by this pass

- **`ThroughputFilterOverride.cs:10`** — removing the method body survived because the mapping had no
  test of its own, and no caller's test told the three answers apart.
  `ThroughputFilterOverrideTest.ToFilterMode_YesAppliesTheFilterNoSkipsItAndLeavingItOutKeepsTheTeamsSetting`
  pins all three: yes applies the filter, no skips it, left out keeps the Team's setting.
- **`ForecastRealityCheckService.cs:113` (`?? []` → `[]`) and `:118` (`== level` → `!= level`)** — no
  test read the held count per level, so dropping every outcome, or counting every *other* level's
  holds, went unnoticed. `ForecastRealityCheckServiceTest.Each_level_counts_only_the_checks_it_held_in_itself`
  gives each horizon an actual that clears one more level than the last, so the four levels hold 4, 8,
  12 and 16 times, and asserts exactly those counts.
- **`RealityCheckVerdictPolicy.cs:12` (`>= 0` → `> 0`, and `All` → `Any`)** — `HasAReadingAtEveryLevel`
  had no direct test; the service test only ever fed it a forecast with no reading anywhere.
  `RealityCheckVerdictPolicyTest.HasAReadingAtEveryLevel_ZeroItemsIsAReadingButMinusOneAtAnyLevelLeavesTheForecastUnreadable`
  pins two decisions: a forecast of 0 items at a level is a legitimate reading, not a degenerate one;
  and a forecast where only *some* levels carry −1 is still unreadable.

#### Accepted survivors

- **`ForecastRealityCheckService.cs:55`, `Append` → `Prepend`** — equivalent. The Team's own window is
  added to the standard ladder and the result goes straight through `.Distinct().Order()`, so where it
  was added cannot change the list.
- **`ForecastRealityCheckService.cs:113`, `?? []` → `cell.LevelOutcomes`** — unreachable. The line is
  behind `.Where(cell => cell.Sufficiency.IsSufficient)`, and the only way a cell becomes sufficient is
  `CheckedWindow.Evaluated`, which always builds its level outcomes. Every unevaluable cell carries
  `null` there and is sufficient `false`. The `?? []` only guards the nullable type.

#### Not mutated

- **`ForecastRealityCheckController.cs`** — covered by the HTTP acceptance tests under
  `API.Integration` (routes, RBAC, 404 / 400, filter status). The config's `test-case-filter` keeps
  them out: each boots a `WebApplicationFactory`, which costs minutes per mutant against about three
  minutes for the whole unit run.
- **`RealityCheckResultDto.cs`, `RealityCheckInputDto.cs`, `RealityCheckVerdictTypes.cs`,
  `IForecastRealityCheckService.cs`** — records, enums and an interface; no logic to mutate.

### Frontend

N/A for this gate. The reality check's UI is being replaced by Story #6094, and the maintainer scoped
this gate to the backend.

### Configuration notes

The `test-case-filter` names `ThroughputFilterOverride` as a substring, so the new
`ThroughputFilterOverrideTest` joins the mutant loop without a config change. It also takes in
`ForecastControllerTest` and `TeamMetricsControllerTest`, the two other callers of the filter
override, and excludes `API.Integration` and `Integration.Containers` for the cost reason above.

## Story 6094 (Forecast Reality Check dialog)

Run 2026-09-27 against `main` @ `7386b8f02`. Gate is 80 % kill rate. The code was frozen for this pass:
the tests listed under *Closed by this pass* were added afterwards and each was proven by applying its
mutant by hand, watching the new test go red and restoring the file. Stryker was not re-run over them,
so the scores below are the run's own.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 9.6.1, vitest runner) | **90.61 %** | 553 | 502 | 51 | 1 | 12 m 39 s |
| Backend (Stryker.NET 5.0.0) | **90.48 %** | 21 | 19 | 2 | 0 | 3 m 06 s |

Configs: `stryker.6094.frontend.json` with `vitest.stryker.6094.config.ts` (both copied to
`Lighthouse.Frontend/` to run), and `stryker.6094.backend.json`, run from `Lighthouse.Backend.Tests/`.

Of the 51 frontend survivors, 11 were missing tests and are closed below. Counting them killed, the
frontend would stand at 513 of 554 mutants, **92.60 %**. The other 40 and the one uncovered mutant are
accepted, each with its reason.

### Frontend

| file | mutants | killed | survived | closed by this pass | left |
| --- | --- | --- | --- | --- | --- |
| realityCheckCopy.ts | 313 | 309 | 4 | 0 | 4 |
| realityCheckGrading.ts | 78 | 75 | 3 | 3 | 0 |
| RealityCheckPeriodGroup.tsx | 61 | 46 | 14 (+1 no coverage) | 2 | 12 (+1) |
| ForecastRealityCheck.tsx | 41 | 33 | 8 | 3 | 5 |
| RealityCheckDialog.tsx | 20 | 16 | 4 | 0 | 4 |
| RealityCheckLegend.tsx | 12 | 3 | 9 | 0 | 9 |
| RealityCheckTable.tsx | 11 | 5 | 6 | 0 | 6 |
| RealityCheckVerdict.tsx | 10 | 9 | 1 | 1 | 0 |
| RealityCheckGradedCell.tsx | 8 | 6 | 2 | 2 | 0 |

#### Closed by this pass

- **A confidence level missing from a check** — `RealityCheckPeriodGroup.tsx:84` (`check === null` →
  `false`) and the three mutants of `realityCheckGrading.ts:39` (`||` → `&&`, and either side → `false`).
  No test sent a check whose level outcomes left a level out, or a check with outcomes but no actual.
  `TeamForecastView.realityCheck.dialog.test.tsx` *a level the check left out stays an empty cell, and
  every other forecast keeps its own column* drops the 70th from one check and asserts the 70th column
  is an empty, unnamed cell while the 50th, 85th and 95th keep their own names under their own columns.
  `realityCheckGrading.test.ts` *one level of one check, found by its percentile* pins `gradedCheckAt`:
  it reads the level asked for, has nothing for a level left out, and nothing for a check with no
  actual even when the level is listed. The server today always lists all four levels and sends the
  outcomes and the actual together, so these cover the client's own guard, not a reply seen in the wild.
- **`RealityCheckPeriodGroup.tsx:48`, `" to "` → `""`** — the period header was only ever matched in
  pieces. *heads each period with its length, its first to its last day, and what was completed* pins
  the whole header, `8 weeks, <first day> to <last day>: 42 Work Items completed`.
- **`ForecastRealityCheck.tsx:19`, the fallback message → `""`** — every failure test rejected with an
  `Error`, whose own message is shown. *a failure that carries no message of its own still says plainly
  that the check could not run* rejects with a plain object and pins the alert to
  `The reality check could not be run. Please try again.`
- **`ForecastRealityCheck.tsx:52` (`[reportUsage]` → `[]`) and `:77` (dependencies → `[]`)** — these
  looked like dependency arrays that could not matter, but they do. Both things they track arrive after
  the tab has mounted: the Team's settings, which decide whether a forecast filter applies, and this
  browser's usage-data answer, which gives the reporter a new identity. Frozen at the first render, the
  check would run every time with no filter override, and would report through the answer the tab
  opened with, or not report at all. The stubs hid it: the tests' team service was absent, so no Team
  ever had a filter, and the stubbed reporter never changed. *a Team with a forecast filter runs the
  check with the backtest's filter choice, which is only known once the Team's settings have come in*
  gives the fixture a team service whose settings carry a filter and asserts the check is asked with the
  override on. *reports through the browser's latest answer, not the one it had when the tab opened*
  swaps the reporter after mounting and asserts only the new one hears the run. `:52` is killed by the
  second; `:77` by both.
- **`RealityCheckVerdict.tsx:27`, `[]` → `["Stryker was here"]`** — the existing test with checks that
  could not run had every counted check share one grade, so padding the count with junk left the
  majority standing. *keeps a grade usual when it is usual among the checks that ran, however many
  others could not run* uses 7 checks within 10% and 5 more than a quarter off: a strict majority of the
  12 checks that ran, but not of 16, so counting the four that could not run drops the "Usually".
- **`RealityCheckGradedCell.tsx:36` and `:39`, `" "` → `""`** — these are not styling. They are the
  spaces between the visible forecast, miss, percentage and held mark; without them `+11` and `26%`
  read as `+1126%`. The accessible name was pinned but the visible text only in pieces. *spaces a graded
  cell's words apart, so the miss never runs into the percentage* pins `31 +11 26% ✓ held` and
  `48 −6 14% ✗ did not hold`.

#### Accepted survivors

**Copy (`realityCheckCopy.ts`)**

- **`:64`, `items.join("")` → `items.join("Stryker was here!")`** — equivalent. That branch is taken
  only for fewer than two items, and `join` puts its separator only *between* items, so for zero or one
  item the separator never appears. `listOf` already has tests for `[]` and `[30]`, and both stay green
  under this mutant.
- **`:46`, `start >= 0` → `true`** — equivalent for any input. The span also needs at least two sound
  windows and a run as long as the sound list. When the first sound window is missing from the ladder,
  `start` is `-1` and `slice(-1, length - 1)` yields at most one element, so the length check already
  fails.
- **`:48`, `run.length === soundWindowDays.length` → `true`** — equivalent for every reply the server can
  send. The server builds the sound windows by filtering the sampled ladder, which is distinct and in
  ascending order, so the sound list keeps the ladder's order. Every sound window after the first
  therefore sits further along the ladder, and the run starting at the first one always has room for
  all of them. Only a sound list naming a window outside the ladder, or out of its order, would tell the
  two apart.
- **`:76`, `region.shape === "list"` → `true`** — unreachable. This function writes the sentence for
  *all windows alike* and *some windows sound*, and the server gives either of those only when at least
  one window held up. So the region there is always a span or a list, never *none*.

**`ForecastRealityCheck.tsx`**

- **`:37`, `useRef(false)` → `useRef(true)`** — equivalent. The flag is read only after a run's answer
  comes back, and every way to start a run goes through opening the dialog, which sets it first. *Run
  again* is only on screen while the dialog is open.
- **`:44`, `[]` → `["Stryker was here"]`** — equivalent. A constant dependency never changes, so the
  cleanup still runs exactly once, on unmount.
- **`:49`, `"answered"` → `""`** — equivalent. The dialog checks a run's state only against `running` and
  `failed`, and shows the answer in every other case, so a blank state still shows it.
- **`:100` (object and string)** — the button's `sx` alignment. Layout only; jsdom does no layout.

**Styling that jsdom cannot see**

- **`RealityCheckLegend.tsx:17`, `:20`, `:26`, `:37`, `:38`, `:39`, `:42` (9)** — the legend's `sx`:
  flex layout, swatch size, border colour, list markers. The legend's words are asserted separately.
- **`RealityCheckTable.tsx:31`, `:54`, `:56` (6)** — header alignment, the horizontal scroll, caption
  side and minimum width. The scroll region's keyboard focus and its name from the caption are asserted;
  the `overflowX` itself is layout.
- **`RealityCheckDialog.tsx:81`, `:87` (3)** — title padding and the close button's absolute position.
- **`RealityCheckDialog.tsx:69`, `"sm"` → `""`** — the breakpoint below which the dialog goes full
  screen. A layout choice, and jsdom has no viewport: the tests stub `matchMedia` with one answer for
  every query, so the narrow-screen test passes whatever the breakpoint.
- **`RealityCheckPeriodGroup.tsx:23`, `:24`, `:27`, `:28` (4)** — the sticky header cells' `sx`
  (position, background, overlay) that keeps the period and window in view while the table scrolls
  sideways.
- **`RealityCheckPeriodGroup.tsx:134`, `+ 1` → `- 1`** — the period header's `colSpan`. With four levels
  it spans 3 columns instead of 5, which only changes where the header is drawn.

**Guards against a check the server never sends (`RealityCheckPeriodGroup.tsx`)**

The server checks every sampling window against every horizon, so each row the table draws has its
check (`cell` is never `undefined`). And a check either ran, carrying both its level outcomes and its
actual, or could not run, carrying neither.

- **`:64` and `:71`, `cell?.sufficiency` → `cell.sufficiency`** — differ only when `cell` is
  `undefined`.
- **`:64`, `"Sufficient"` → `""`** (no coverage) — the fallback reason for a missing cell. Never reached;
  and a check that ran never reaches this component either, so the reason is always one of the two
  "could not run" ones.
- **`:93` and `:94`, `cell?.` → `cell.`** — differ only when `cell` is `undefined`.
- **`:95`, `||` → `&&`, and either side → `false`** — differ only when exactly one of level outcomes and
  actual is missing.

### Backend

| file | tested | killed | survived |
| --- | --- | --- | --- |
| ForecastRealityCheckService.cs | 21 | 19 | 2 |

The two survivors are the known equivalents from Story 6072 above, at new line numbers:
**`:59`, `Append` → `Prepend`** (was `:55`; the list goes straight through `.Distinct().Order()`) and
**`:114`, `?? []` removed** (was `:113`; only sufficient cells reach it, and every sufficient cell
carries its level outcomes).

### Not mutated

- **`src/models/Forecasts/RealityCheckResult.ts`** — holds only the list of grade names and the zod
  schemas that parse the server's reply. No logic to mutate.

## Story 6094 — US-06 (at a glance)

Run 2026-09-27 against `main` @ `dd89f6b8a`. Gate is 80 % kill rate. The code was frozen for this pass:
the tests listed under *Closed by this pass* were added afterwards and each was proven by applying its
mutant by hand, watching the new test go red and restoring the file. Stryker was not re-run over them,
so the scores below are the run's own.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 9.6.1, vitest runner) | **84.22 %** | 639 | 539 | 100 | 1 | 31 m 59 s |
| Backend | N/A | — | — | — | — | — |

Config: `stryker.6094.frontend.json` with `vitest.stryker.6094.config.ts` (both copied to
`Lighthouse.Frontend/` to run).

The backend was not mutated for this change. Its only backend edit is one Information log line in
`ForecastRealityCheckController.cs`; the controller is covered by HTTP acceptance tests, which are kept
out of Stryker.NET because a mutation run over them costs far more than it tells, and that log line is
already pinned by the acceptance scenarios that read the log.

Of the 100 frontend survivors, 11 were missing tests and are closed below. Counting them killed, the
frontend would stand at 550 of 640 mutants, **85.94 %**. The other 89 and the one uncovered mutant are
accepted, each with its reason.

### Frontend

| file | mutants | killed | survived | closed by this pass | left |
| --- | --- | --- | --- | --- | --- |
| realityCheckCopy.ts | 243 | 240 | 3 | 0 | 3 |
| realityCheckGrading.ts | 128 | 127 | 1 | 1 | 0 |
| RealityCheckPeriodGroup.tsx | 60 | 40 | 19 (+1 no coverage) | 0 | 19 (+1) |
| ForecastRealityCheck.tsx | 43 | 36 | 7 | 0 | 7 |
| RealityCheckLevelRow.tsx | 32 | 5 | 27 | 6 | 21 |
| useFocusTooltip.ts | 27 | 23 | 4 | 4 | 0 |
| RealityCheckDialog.tsx | 26 | 20 | 6 | 0 | 6 |
| RealityCheckLegend.tsx | 25 | 11 | 14 | 0 | 14 |
| RealityCheckHeadline.tsx | 17 | 11 | 6 | 0 | 6 |
| RealityCheckWindowBadge.tsx | 13 | 10 | 3 | 0 | 3 |
| RealityCheckGradedCell.tsx | 10 | 5 | 5 | 0 | 5 |
| RealityCheckSummary.tsx | 6 | 6 | 0 | 0 | 0 |
| RealityCheckTable.tsx | 6 | 3 | 3 | 0 | 3 |
| RealityCheckLevelLabel.tsx | 4 | 2 | 2 | 0 | 2 |

#### Closed by this pass

- **Where a level's tick sits** — `RealityCheckLevelRow.tsx:36` (the tick's style → nothing, and its
  object → `{}`), `:37` (`"absolute"` → `""`), `:38` (the `left` offset → an empty string) and `:81`
  (the bar box's style → `{}`, and `"relative"` → `""`). The spec's title promised a tick at 85%, but it
  only asserted the bar's value and its text alternative. Each of these leaves the tick somewhere other
  than the rate the level should hold at, which is the one thing the tick shows. *each level's tick sits
  along its own bar at the rate the level should hold at* reads the computed style of every bar and
  asserts a relatively positioned bar holding an absolutely positioned tick at 50%, 70%, 85% and 95%.
- **Only Escape closes a tooltip** — `useFocusTooltip.ts:20` (`event.key === "Escape"` → `true`). While
  a tooltip is open the hook catches keys on the whole document ahead of the dialog; treating every key as
  Escape closed the tooltip on any key and swallowed that key. *only Escape closes the badge's tooltip: a
  reader pressing an arrow key to scroll keeps it open* focuses the badge, presses an arrow key and asserts
  the badge is still described by its tooltip.
- **The Escape listener leaves with the tooltip** — `useFocusTooltip.ts:26`, three mutants of the
  cleanup: it does nothing, it names no event, or it names the wrong phase. Each leaves the listener
  behind, still stopping Escape after the tooltip has gone, so the reader can no longer close the dialog
  with the key. *once a cell's tooltip has closed, Escape closes the dialog again* hovers a cell, moves
  away until the tooltip is gone, presses Escape and asserts the dialog closes.
- **`realityCheckGrading.ts:55`, `[]` → `["Stryker was here"]`** — `gradedChecksAt` had no test of its
  own, and its one caller only counts checks within 10%, which a filler entry never is. *keeps only the
  checks that have a reading at that level, and nothing in place of the others* passes a check read at the
  50th, one read only at the 95th, one that could not run and one more read at the 50th, and asserts
  exactly the two 50th readings come back.

#### Accepted survivors

**Equivalent**

- **`realityCheckCopy.ts:23`, `items.join("")` → `items.join("Stryker was here!")`** — that branch is
  taken only for fewer than two items, and `join` puts its separator only between items, so it never
  appears.
- **`realityCheckCopy.ts:209` and `:216`, `miss > 0` → `miss >= 0`** — the sign and the word "more" or
  "fewer" are only written for a miss. A miss of zero returns earlier with its own sentence, so neither
  helper ever sees zero.
- **`RealityCheckLevelRow.tsx:64`, the not-tested reading → `false`, and `"NotEvaluated"` → `""`** —
  both drop the check on the server's reading and fall back to working the share out from the counts.
  The server reads a level as not tested exactly when no check could run, and every level shares that
  one count, so the counts give no share either: the row still says it was not tested and the bar stays
  empty.
- **`RealityCheckWindowBadge.tsx:42`, `notChecked === null` → `false`** — when every window could be
  checked this draws an empty box inside the tooltip instead of nothing. It holds no text, so the tooltip
  reads and describes the badge exactly as before.
- **`ForecastRealityCheck.tsx:39`, `useRef(false)` → `useRef(true)`** — the flag is read only after a
  run's answer comes back, and every way to start a run goes through opening the dialog, which sets it
  first.
- **`ForecastRealityCheck.tsx:46`, `[]` → `["Stryker was here"]`** — a constant dependency never
  changes, so the cleanup still runs exactly once, on unmount.
- **`ForecastRealityCheck.tsx:51`, `"answered"` → `""`** — the dialog checks a run's state only against
  `running` and `failed`, and shows the answer in every other case.

**Guards against a check the server never sends (`RealityCheckPeriodGroup.tsx`)**

The server checks every sampling window against every horizon, so each row the table draws has its
check, and a check either ran, carrying both its level outcomes and its actual, or could not run,
carrying neither.

- **`:51` and `:58`, `cell?.sufficiency` → `cell.sufficiency`**, and **`:80` and `:81`, `cell?.` →
  `cell.`** — differ only when a row has no check.
- **`:51`, `"Sufficient"` → `""`** (no coverage) — the fallback reason for a row with no check. Never
  reached.
- **`:82`, `||` → `&&`, and either side → `false`** — differ only when exactly one of the level outcomes
  and the actual is missing.

**Styling that jsdom cannot see**

- **`RealityCheckLevelRow.tsx` (19)** — the bar's rounding, track and fill colours and its
  switched-off transition (`:29`, `:32`, `:33` ×3); how far the tick overhangs the bar and its colour
  (`:26`, `:39`, `:40`, `:42`); the row's stacking, spacing and alignment (`:71` ×3, `:72`, `:73` ×3);
  the label column's width (`:75`); and the bar's width (`:81` ×2). Where the tick sits and how full the
  bar is are asserted; these only change how they look.
- **`RealityCheckLegend.tsx` (14)** — the legend's flex layout, gaps, swatch border, title weight and
  width, and the list reset (`:14`, `:23`, `:33`–`:36`, `:41`, `:46`–`:51`). The legend's words, order
  and swatch fills are asserted.
- **`RealityCheckGradedCell.tsx:10`, `:11` (5)** — the focus outline drawn on a cell reached by
  keyboard. jsdom does not match `:focus-visible`, so the outline has no computed value to read; that a
  cell is a keyboard stop, and what it is named, are asserted.
- **`RealityCheckHeadline.tsx:35`, `:52`, `:57` (6)** — the headline row's alignment, where the
  explanation's popover is anchored, and its padding and width. jsdom does no layout; that the popover
  opens on a press, takes focus, holds the explanation and the article, and closes on Escape is asserted.
- **`RealityCheckDialog.tsx:76`, `:82`, `:92` (5)** — the title's padding, the close button's absolute
  position and the status row's alignment.
- **`RealityCheckDialog.tsx:64`, `"sm"` → `""`** — the breakpoint below which the dialog goes full
  screen. jsdom has no viewport: the tests stub `matchMedia` with one answer for every query.
- **`ForecastRealityCheck.tsx:100`–`:103` (4)** — the trigger row's flex alignment.
- **`RealityCheckPeriodGroup.tsx:22`–`:27`, `:37`–`:40` (11)** — the sticky header cells (position,
  background, overlay) and the period header's alignment on narrow and wide screens.
- **`RealityCheckPeriodGroup.tsx:121`, `+ 1` → `- 1`** — the period header's `colSpan`. With four levels
  it spans 3 columns instead of 5, which only changes where the header is drawn.
- **`RealityCheckTable.tsx:31`, `:33` (3)** — the horizontal scroll and the table's minimum width. The
  scroll region's name and keyboard focus are asserted.
- **`RealityCheckWindowBadge.tsx:51` (2)** — the badge's alignment in its column.
- **`RealityCheckLevelLabel.tsx:17` (2)** — the label row's alignment.

### Not mutated

- **`src/models/Forecasts/RealityCheckResult.ts`** — holds only the list of grade names, level readings
  and the zod schemas that parse the server's reply. No logic to mutate.
- **Backend** — see above; the one backend line is a log statement in a controller covered only by HTTP
  acceptance tests.
