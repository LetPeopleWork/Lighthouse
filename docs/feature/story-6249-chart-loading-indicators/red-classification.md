# RED classification — story-6249-chart-loading-indicators

Produced by the pre-DELIVER fail-for-the-right-reason gate, 2026-10-09, **re-run after the review-round-2 revision**.
Every pending specification was un-skipped (in throw-away copies of the six files, `it.skip` → `it`), run against
unmodified production code plus the `widgetStatus.ts` scaffold, classified, and the copies deleted. DELIVER reads this
at PREPARE to confirm the RED it starts from is genuine.

**Verdict: PASS.** Zero `IMPORT_ERROR`, zero `FIXTURE_BROKEN`, zero `SETUP_FAILURE`, zero
`OBSERVABLE_NOT_AT_PORT`. Every pending specification is `MISSING_FUNCTIONALITY`.

Run un-skipped: **253 cases, 232 failed, 21 passed.** The 21 are the 20 pins plus one pending specification that
reds the run another way (below). Every suite loaded; no failure came from an import, a fixture or the held-answer
harness. A probe run (the chart loops' status assertion swapped for a presence check) confirmed that every chart the
"every" and "only" specifications loop over is framed at the moment they look, so those loops fail on the missing
status, never on a chart that is not there.

## How the 233 pending specifications fail

| Failure shape | Cases | Classification | Why it is the right reason |
|---|---|---|---|
| `Error: Not yet implemented -- RED scaffold` (all of `widgetStatus.test.ts` but one; the self-fetching child in `WidgetShell.status.test.tsx`) | 101 + 8 | `MISSING_FUNCTIONALITY` | The precedence rule, the per-widget status, the request-progress mapping, the Cumulative Time per State choices, the every-request failure table and the reporter do not exist; the scaffold is reached through the real import |
| A chart drawn outside a frame throws (`expected [Function] to not throw`) | 1 | `MISSING_FUNCTIONALITY` | The reporter hook is the scaffold; it must do nothing outside a frame |
| `data-widget-status` is `null` where `loading` / `ready` / `error` is expected (the loops stop at the first chart: `cycleScatter: expected null …`) | 48 dashboard + 24 frame, card, details, over-time | `MISSING_FUNCTIONALITY` | The frame draws no status yet. The tests reach the assertion after real fetches, real window changes and real answers |
| Widget keys before data ≠ the category's placement; a frame not found on a first visit (`widget-shell-totalThroughput`, `widget-shell-throughput`); Estimation vs. Cycle Time / Feature Size not framed after their first answer failed | 14 + 2 + 2 | `MISSING_FUNCTIONALITY` | Today a widget is absent until its data lands, and those two stay away on a failure; slice 02 changes both |
| The body box `widget-shell-body-<key>` not found where a control inside a loading chart must not respond | 4 | `MISSING_FUNCTIONALITY` | The body carries no test id and no pointer block yet; slice 01b adds both on the element the maintainer named |
| A chart reading another window's data (quoted below) | 11 | `MISSING_FUNCTIONALITY` | The reported defect and the pre-existing stale-window bugs folded into slice 01a, reproduced |
| A request that should be sent is not (quoted below), or no query exists to read options from (`expected 0 to be greater than 0`: 8 dashboard rows, the run chart card, the details widget) | 3 + 10 | `MISSING_FUNCTIONALITY` | The filtered views and the picker never ask again for a new window; nothing is a query yet |
| View Data enabled while loading; a click on the dimmed chart is accepted (`promise resolved "undefined" instead of rejecting`); a rating still shown on a failed frame; a second button (View Data) beside info on a failed frame | 4 | `MISSING_FUNCTIONALITY` | No pointer block on the body, no header chrome by state yet |
| The picker's Work Items failing: every assertion holds, and Vitest reports `Unhandled Rejection: getCumulativeStateTimeCandidatesForTeam failed` against the run | 1 | `MISSING_FUNCTIONALITY` | Today that request has no `catch`; slice 01a adds it in the commit that un-skips this specification. The run, not the assertion, is red |

The defects, quoted back:

| Specification | What it failed on |
|---|---|
| shows the second window when the first window's answer arrives last | `Work Items Completed: 90` where `…: 7` was expected — the reported "old window lands last and stays" |
| answers arriving in four orders where the last window picked does not answer last | `…: 14` or `…: 90` where `…: 7` was expected — whichever window answered last wins today |
| a window change drops the chosen stretch, as it does today, and a late answer for it never shows | the old stretch's numbers after the reader moved to 90 days — the unguarded scope fetch |
| Work Items chosen in the picker stay chosen and are counted again / a late choice never replaces the current one | `Work Items 11+12, 30 days ending 0 days ago` — the selection is never refetched for the new window |
| the picker offers the new window's Work Items once the window changes | `ITEM-11 (30 days ending 0 days ago)` — candidates are fetched once per mount |
| the picker asks for no Work Items of the new window until the reader opens it again | no request on reopening — the "already asked" flag never resets |
| the filtered series / score of a window the reader has left never replaces the current one | `3` and `0.31` (old window) where `9` and `0.91` were expected |
| with the filter on, a new window fetches the filtered series / score for that window | no request for the new window — both filtered views fetch once and keep the result |

## Pins (pass today, run in CI from now on)

| Specification | File | Why it passes today, and why it stays |
|---|---|---|
| a frame left at its defaults shows its chart and header as before | `WidgetShell.status.test.tsx` | Every caller that passes no status must keep today's frame |
| keeps the title, rating and trend at full strength, outside anything that is faded | same | Nothing is faded today; guards the header against being dimmed with the body (mutation-checked, below) |
| still explains the chart when the reader asks | same | The info button must stay usable while the body refuses the pointer |
| the could-not-load message reads exactly as agreed | same | The one literal pin of the copy; every other spec imports the constant |
| with the filter off, a new window shows the dashboard's own series / score for it (× 2) | `ThroughputRunChartCard.loading.test.tsx`, `PredictabilityScoreDetailsWidget.loading.test.tsx` | The unfiltered data comes from the page; no extra request may appear |
| an empty answer reads as nothing recorded yet, never as a spinner (× 2) | `OverTimeWidgets.loading.test.tsx` | The over-time empty state must survive the new spinner |
| switching to another selection and back within one window asks for nothing new (× 2) | same | The over-time charts keep today's per-selection cache; they are not moved to queries |
| answers arriving in the two orders where the last window picked answers last (× 2) | `BaseMetricsView.loading.test.tsx` | The in-order case must stay right while the out-of-order cases are fixed |
| until a chart can say it could not be loaded, a failed window leaves its older chart in place | same | Today's behaviour, which slice 01a keeps so that it changes nothing visible. **Slice 01b deletes this pin** in the commit that un-skips "stops its spinner, removes the older chart and says it couldn't be loaded" |
| a {team, portfolio}'s Portfolio & Features adds and removes no chart once its data has arrived (× 2) | same | With Estimation vs. Cycle Time and Feature Size left out, the category's one always-placed chart is framed before its data already |
| Estimation vs. Cycle Time not framed while its first answer is on its way; stays away when not set up; stays away on a window change | same | Today's presence rule, kept |
| Feature Size not framed until its Features arrive; stays away when the window holds none | same | Today's presence rule, kept |

The four round-1 pins that said a control inside a loading chart still works (frame, run chart card, Predictability
Score details, dashboard) are gone: the maintainer chose to block the whole body. Each became a pending specification
that the control does not respond.

## Mutation check of the header pin

The pin "keeps the title, rating and trend at full strength, outside anything that is faded" only guards the header
if it goes red when the header fades. Checked with a throw-away mutation on 2026-10-09:

1. `Lighthouse.Frontend/src/pages/Common/MetricsView/WidgetShell.tsx`, the header box
   (`widget-shell-header-<key>`, line 250): `display: "flex",` became `opacity: 0.4, display: "flex",`.
2. `pnpm vitest run src/pages/Common/MetricsView/WidgetShell.status.test.tsx -t "keeps the title, rating and trend"`
   → `× keeps the title, rating and trend at full strength, outside anything that is faded`,
   `AssertionError: expected <div …(2)>…(5)</div> to be null` (the faded header box found as an ancestor),
   `Tests 1 failed | 25 skipped (26)`.
3. Reverted with `git checkout -- Lighthouse.Frontend/src/pages/Common/MetricsView/WidgetShell.tsx`; `git diff` on
   the file then printed 0 lines, and `git status` no longer lists it.

Result: the pin kills the mutant. It stays a pin.

## Suite state at hand-off

Committed with every pending specification skipped: `pnpm test` → `Test Files 445 passed | 1 skipped (446)`,
`Tests 6575 passed | 233 skipped (6808)`. `pnpm build` is recorded in feature-delta.md under
`DISTILL / [REF] Pre-requisites`.
Re-run this gate by copying a file, replacing `it.skip` with `it`, and running the copy with
`pnpm vitest run <copy>`; delete the copy afterwards.
