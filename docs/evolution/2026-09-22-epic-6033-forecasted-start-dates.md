# Epic 6033 — Sync forecasted start dates with the work tracking system

**Merged to `main`, not yet released** · ADO Epic #6033 (`Community`, `Productboard`, `Release Notes`),
raised by Chris Graves / Focusrite · workspace `docs/feature/epic-6033-forecasted-start-dates/` ·
commits `2e3bdad81..0e510cd0c` (2026-09-19 → 2026-09-21, ~77 commits) · seven Stories, one Bug, all
`Closed`; the Epic sits at `Resolved` until the release goes out.

## What users get

Lighthouse has always said when a Feature will *finish*. It now also says when it will **start**, and
draws the whole plan.

- **The Feature table** carries a **Forecasted Start** column beside the completion one, at the same
  four probabilities, on every licence. A Feature already under way shows the day it really started.
- **The work tracking system** can receive that day: four new Data Sync value sources
  (`ForecastedStartPercentile50/70/85/95`), under the existing premium gate.
- **A Delivery draws as a timeline** (premium): one bar per Feature from its start to its completion,
  a tinted target-date column, today's edge, dependency lines between bars, Team rows under a bar,
  and a colour saying whether a bar is finished, finishes late, or does not even begin before the
  target date.

## The mechanism in the Epic description was rejected, and that is the decision the rest hangs on

The request described deriving a Feature's start from the **preceding** Feature's forecast completion.
That was dropped on 2026-09-19: the Monte Carlo run already names the row it works on each simulated
day (`SimulatedRun.WorkOneDayOf`), so the day a Feature is first pulled is **recorded**, not derived.
Deriving it would have produced a second, differently-computed answer to a question the run had already
answered — and the two would have disagreed the moment Feature WIP, dependencies or blackout days
entered the picture.

Two consequences worth keeping:

- **At Feature WIP 1 the next Feature starts the same day the previous one ends.** Bars abut. A team
  that finishes mid-day pulls the next item the same day, so there is no gap to draw.
- **The run does not know what is physically in progress** — it reads the board's order (ADR-202). A
  not-started Feature ranked above work in flight forecasts a start that is too early. This is on
  purpose, and is documented in `docs/portfolios/detail.md` so it does not arrive as a bug report.

## What shipped, slice by slice

| Slice | Story | What it added |
|---|---|---|
| 01 | #6045 | The run records the pull day per trial, at two grains — per Feature and per Feature-and-Team (ADR-199), stored in `Feature.StartForecasts`, a collection of its own (ADR-200) |
| 02 | #6046 | The `Forecasted Start` column, with three answers: an observed day, four percentiles, or nothing |
| 03 | #6047 | Four write-back sources, sharing the existing percentile plumbing; the observed day wins, and nothing is written when nothing can be said |
| 04 | #6048 | The Delivery Timeline tab, built on `@svar-ui/react-gantt` |
| 05 | #6049 | Dependency lines — drawn only where the forecast acted on the wait (ADR-203) |
| 06 | #6050 | Team rows under a multi-Team bar, and a Team's colour on a single-Team bar (ADR-204) |
| — | #6054 | Bug: a started or finished Feature could show a future start. State decides whether work began; the date only fills in the value. Reverses AC-2.3 and AC-3.4 of this Epic |
| 07 | #6067 | One question at a time: `Show [Nothing][Teams][Status][Warnings]`, default **Status** (ADR-205) |

`Feature.Forecast` aggregates `Feature.Forecasts` unconditionally, so a start distribution added to that
collection would have silently corrupted every completion forecast. Separate identity was not tidiness.

## Buying a Gantt chart

**MUI X Gantt does not exist** — its own documentation says so. No licence was ever available to buy.
The chart is `@svar-ui/react-gantt` 2.7.3, free and MIT with 25 MIT dependencies, and everything this
Epic needs from it is in the free edition: dependency arrows with automatic routing, tooltips,
`readonly`, zoom. Vertical markers, unscheduled tasks and export are PRO-only, which is why the target
date and today are drawn as axis columns rather than as markers.

**Exactly one production file imports the vendor** — `DeliveryGanttChart.tsx` — and
`ganttAdapterBoundary.enforcement.test.ts` scans `src` and fails naming any second one. Test files are
exempt so they can stand the library in.

Every one of the vendor's traps was silent, and each cost a live review round: a theme element that
declares its CSS variables **on itself** (so setting them on an ancestor does nothing); a scale `format`
given as a string that is printed verbatim and shipped to a screenshot as `MMMM yyyy`; `autoScale`
defaulting to true and quietly discarding the range it was handed; `getContext("2d")` returning null in
jsdom so any mount throws; an `on${string}` index signature that typechecks a misremembered event name
and then never fires.

## The encoding that was built, measured, and thrown away

Slice 07 first drew finished and late as **caps on the ends of a bar**. It was implemented, tested,
mutation-tested twice, and rejected on sight when the maintainer saw it against a real Delivery: too
small to read, and answering three questions at once was the wrong goal.

What shipped instead is one exclusive choice and a whole-bar fill. The precedence came back with it —
one bar wears one colour, so `finished > startsAfterTarget > endsAfterTarget` is ranked again, where
caps had let each end answer separately. ADR-205 was rewritten and renamed the same day.

Two colour collisions are accepted, both settled by looking rather than by argument: late amber is the
target band's own `#ff9800`, and `finished` `#388e3c` is a *second green* against the default bar fill.
If the greens ever stop reading apart, the one to move is the **default fill** — it belongs to every bar
on every Delivery, `finished` belongs to a case.

## Mutation ledger

Full per-file tables: `docs/evolution/epic-6033-forecasted-start-dates/mutation-results.md`.

| Slice | Headline | Slice-owned | Note |
|---|---|---|---|
| 01 | 73.54 % BE | **93.83 %** | whole-file score counts four large pre-existing files |
| 02 | **93.88 %** FE | — | |
| 03 | **93.94 %** BE · **100 %** FE | — | |
| 04 | **82.69 %** FE | — | two rounds |
| 05 | 72.54 % | **92.22 %** | |
| 06 | 79.94 % | **90.59 %** | headline fell **because a well-covered block was deleted** on request; the owned figure rose in the same run |
| 07 | **79.96 %** | ~90 % | one mutant short of the gate, reported rather than closed |

Slice 07 stopping 0.04 % short is the ledger's own conclusion and is worth preserving: every survivor
left in the files the slice owns is an `sx` declaration or a defensive attribute. A test for one of them
would have been written for the number, not for the product — and narrowing the mutate list to the files
the slice created would read ~90 % without a single test changing, which is the move this ledger caught
itself making once already.

**`concurrency: 2` was halving mutation throughput.** Runs 3 and 4 of slice 07 differ by that one value:
11m19s → 5m30s on a twelve-core machine. All eight Stryker configs in the repository carry it, and it
reads as one file copied forward rather than a number anyone chose. `coverageAnalysis` stays `"off"`
deliberately — the preference stores are module-level singletons, and per-test attribution would invent
survivors.

## What this Epic kept teaching

**Assertions that cannot fail, four of them in one slice**, each caught by a different instrument — the
RED measurement, an adversarial review, hand-sabotage, and mutation. The sharpest asserted a CSS variable
on the element *we set* rather than on the one that resolves it; even a rewrite reading the rendered
value still passed, because jsdom mocks the vendor stylesheet away. That class is only catchable in a
browser.

**`Storage.prototype` spies are inert in this test environment.** `localStorage` does not inherit from
`Storage.prototype` here; `getItem`/`setItem` are own properties. `vi.spyOn(Storage.prototype, …)`
installs, reports itself installed, and intercepts nothing — and `vi.restoreAllMocks()` does not restore
it, so a broken `setItem` leaks into the next test. Three tests elsewhere in the codebase were found
green and testing nothing, and were fixed here.

**An adversarial review that could *run* the code found six proven defects the four-reviewer gate had
passed**, including a Team's colour changing when the reader moved the probability (positional
assignment over a shrinking key set), two un-nameable Teams sharing one React key, and an
`expect(f(a)).toEqual(f(b))` where `a` **is** `b` — re-introducing a `ci-learnings.md` rule filed two
days earlier.

Two rules were added to `docs/ci-learnings.md` from this work: MUI's `Switch` answers to `role="switch"`,
not `"checkbox"`, and `Array.prototype.at()` is outside this project's configured TS lib — invisible to
Vitest and Biome, caught only by `tsc -b`.

**`.gitignore:463` silently eats the Stryker JSON *configs*, not just the reports.** Its own comment
claims the two are named apart by a dot versus a hyphen; no file in that folder follows that convention.
Slices 04 and 05 had their configs committed only after the fact, and slice 06's needed `git add -f`.

## Docs and demo data

`docs/portfolios/detail.md` now documents the Forecasted Start column and the timeline in full — the
`Show` group and why a button is absent rather than inert, the three status colours and their ranking,
Team rows, warnings on a bar, and what a line between two bars means. `docs/features/features.md`,
`docs/teams/detail.md` and `docs/settings/worktrackingsystems.md` carry the column and the four write-back
sources. `docs/assets/features/deliveryTimeline.png` was regenerated: the old shot predated the
dependency lines and the controls row entirely.

A demo-data defect was found and repaired here: multi-Team Features existed only in the seeded demo
scenario, and their absence had falsified slice 05's own worked example.

## Carries forward

- **ADR-204's title still says *one switch*** — a control slice 07 replaced. All seven ADRs (199–205) are
  now Accepted, and 204 carries a dated amendment saying its control, and only its control, is superseded
  by ADR-205. The title is left as written because renaming a file breaks every link to it.
- **ADR-156 (per-trial completion recording) stays deferred.** It was not un-deferred by this Epic.
- **Two unmapped states in the demo CSVs** vanish silently: `Analyzing` (against the configured
  `Analysing`) ×6 in `Team Gravity.csv`, `Nebula` ×1 in `Team Zenith.csv`. Unrepaired, undecided.
- **No Playwright spec for the timeline** beyond the screenshot test — the maintainer's explicit call.
  The Teams and Warnings views have no screenshot of their own.
- **Slice 05's hand-written `e2s` routing enum** draws nothing and raises nothing when wrong. Verified
  once by eye, never re-asked.
- **`concurrency: 2` in all eight Stryker configs**, worth one edit each.
