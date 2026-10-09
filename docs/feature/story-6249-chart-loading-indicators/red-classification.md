# RED classification — story-6249-chart-loading-indicators

Produced by the pre-DELIVER fail-for-the-right-reason gate, 2026-10-09, **re-run after the review-round-1 revision**.
Every pending specification was un-skipped (in throw-away copies of the six files), run against unmodified production
code plus the `widgetStatus.ts` scaffold, classified, and the copies deleted. DELIVER reads this at PREPARE to confirm
the RED it starts from is genuine.

**Verdict: PASS.** Zero `IMPORT_ERROR`, zero `FIXTURE_BROKEN`, zero `SETUP_FAILURE`, zero
`OBSERVABLE_NOT_AT_PORT`. Every pending specification is `MISSING_FUNCTIONALITY`.

Run un-skipped: **201 cases, 177 failed, 24 passed.** The 24 are the 23 pins plus one pending specification that
reds the run another way (below). Every suite loaded; no failure came from an import, a fixture or the held-answer
harness.

## How the 178 pending specifications fail

| Failure shape | Cases | Classification | Why it is the right reason |
|---|---|---|---|
| `Error: Not yet implemented -- RED scaffold` (all of `widgetStatus.test.ts`; the self-fetching child in `WidgetShell.status.test.tsx`) | 67 + 8 | `MISSING_FUNCTIONALITY` | The precedence rule, the per-widget status, the request-progress mapping and the reporter do not exist; the scaffold is reached through the real import |
| `data-widget-status` is `null` where `loading` / `ready` / `error` is expected | 65 | `MISSING_FUNCTIONALITY` | The frame draws no status yet. The tests reach the assertion after real fetches, real window changes and real answers |
| Widget keys before data ≠ the category's placement (e.g. `['cycleScatter','aging', …(2)]` against ten), or `widget-shell-<key>` not found on a first visit | 14 + 2 | `MISSING_FUNCTIONALITY` | Today a widget is absent until its data lands, which slice 02 removes |
| A chart reading another window's data (quoted below) | 11 | `MISSING_FUNCTIONALITY` | The reported defect and the pre-existing stale-window bugs folded into slice 01a, reproduced |
| A request that should be sent is not (quoted below), or no query exists to read options from (`expected 0 to be greater than 0`, × 3) | 6 | `MISSING_FUNCTIONALITY` | The filtered views and the picker never ask again for a new window; nothing is a query yet |
| View Data enabled while loading; a click on the dimmed chart is accepted (`promise resolved "undefined" instead of rejecting`); rating still shown in a failed frame; a second button (View Data) beside info in a failed frame | 4 | `MISSING_FUNCTIONALITY` | No pointer block on the chart surface, no header chrome by state yet |
| The picker's Work Items failing: every assertion holds, and Vitest reports `Unhandled Rejection: getCumulativeStateTimeCandidatesForTeam failed` against the test, failing the run | 1 | `MISSING_FUNCTIONALITY` | Today that request has no `catch`; slice 01a's query handles it. The run, not the assertion, is red |

The defects, quoted back:

| Specification | What it failed on |
|---|---|
| shows the second window when the first window's answer arrives last | `Work Items Completed: 90` where `…: 7` was expected — the reported "old window lands last and stays" |
| answers arriving in four orders where the last window picked does not answer last | `…: 14` or `…: 90` where `…: 7` was expected — whichever window answered last wins today |
| a stretch chosen for a window the reader has left never shows its numbers | `scope 7, 30 days ending 0 days ago` after the reader moved to 90 days — the unguarded Cumulative State Time scope fetch |
| Work Items chosen in the picker stay chosen and are counted again / a late choice never replaces the current one | `Work Items 11+12, 30 days ending 0 days ago` — the selection is never refetched for the new window |
| the picker offers the new window's Work Items once the window changes | `ITEM-11 (30 days ending 0 days ago)` — candidates are fetched once per mount |
| the picker asks for no Work Items of the new window until the reader opens it again | no request on reopening — the "already asked" flag never resets |
| the filtered series / score of a window the reader has left never replaces the current one | `3` and `0.31` (old window) where `9` and `0.91` were expected — the run chart's and the Predictability Score details' filtered views |
| with the filter on, a new window fetches the filtered series / score for that window | no request for the new window — both filtered views fetch once and keep the result |

## Pins (pass today, run in CI from now on)

| Specification | File | Why it passes today, and why it stays |
|---|---|---|
| a frame left at its defaults shows its chart and header as before | `WidgetShell.status.test.tsx` | Every caller that passes no status must keep today's frame |
| keeps the title, rating and trend at full strength, outside anything that is faded | same | Nothing is faded today; guards the header against being dimmed with the chart |
| a switch inside the chart still works while the chart is loading | same | Nothing blocks the pointer today; guards in-body controls against a block on the whole body |
| still explains the chart when the reader asks | same | The info popover must stay usable while the chart is loading |
| the could-not-load message reads exactly as agreed | same | The one literal pin of the S3 copy; every other spec imports the constant |
| with the filter off, a new window shows the dashboard's own series / score for it (× 2) | `ThroughputRunChartCard.loading.test.tsx`, `PredictabilityScoreDetailsWidget.loading.test.tsx` | The unfiltered data comes from the page; no extra request may appear |
| the filter switch still works while the dashboard has the chart / score loading (× 2) | same two files | In-body controls stay usable while loading |
| an empty answer reads as nothing recorded yet, never as a spinner (× 2) | `OverTimeWidgets.loading.test.tsx` | The over-time empty state must survive the new spinner |
| switching to another selection and back within one window asks for nothing new (× 2) | same | The per-selection cache the over-time charts ship today stays |
| the Throughput filter switch inside a chart behind the window still asks for the filtered series | `BaseMetricsView.loading.test.tsx` | In-body controls stay usable on the real dashboard |
| answers arriving in the two orders where the last window picked answers last (× 2) | same | The in-order case must stay right while the out-of-order cases are fixed |
| a {team, portfolio}'s Portfolio & Features adds and removes no chart once its data has arrived (× 2) | same | With Estimation vs. Cycle Time and Feature Size left out, the category's one always-placed chart is framed before its data already |
| Estimation vs. Cycle Time not framed while its first answer is on its way; stays away when not set up; stays away on a window change | same | Today's presence rule, kept |
| Feature Size not framed until its Features arrive; stays away when the window holds none | same | Today's presence rule, kept |

## Suite state at hand-off

Committed with every pending specification skipped: `pnpm test` → `Test Files 445 passed | 1 skipped (446)`,
`Tests 6578 passed | 178 skipped (6756)`. `pnpm build` is recorded in feature-delta.md under
`DISTILL / [REF] Pre-requisites`.
Re-run this gate by copying a file, replacing `it.skip` with `it`, and running the copy with
`pnpm vitest run <copy>`; delete the copy afterwards.
