# RED classification — story-6249-chart-loading-indicators

Produced by the pre-DELIVER fail-for-the-right-reason gate, 2026-10-09. Every pending specification was
un-skipped (in throw-away copies of the five files), run against unmodified production code plus the
`widgetStatus.ts` scaffold, classified, and the copies deleted. DELIVER reads this at PREPARE to confirm the RED
it starts from is genuine.

**Verdict: PASS.** Zero `IMPORT_ERROR`, zero `FIXTURE_BROKEN`, zero `SETUP_FAILURE`, zero
`OBSERVABLE_NOT_AT_PORT`. Every failure is `MISSING_FUNCTIONALITY`.

Run un-skipped: **153 cases, 146 failed, 7 passed.** Every suite loaded; no failure came from an import, a
fixture or the held-answer harness.

## How the 146 failures fail

| Failure shape | Cases | Classification | Why it is the right reason |
|---|---|---|---|
| `Error: Not yet implemented -- RED scaffold` (`widgetStatus.test.ts`, the `useReportWidgetStatus` child in `WidgetShell.status.test.tsx`) | 65 + 1 + 7 | `MISSING_FUNCTIONALITY` | The precedence rule, the per-widget status, the inapplicable-key rule and the reporter do not exist; the scaffold is reached through the real import |
| `data-widget-status` is `null` where `loading` / `ready` / `error` is expected | 43 | `MISSING_FUNCTIONALITY` | The frame does not draw a status yet. The tests reach the assertion after real fetches, real window changes and real answers |
| Widget keys before data ≠ the category's placement (`expected ['cycleScatter','aging', …(2)] to deeply equal […(8)]`), or `widget-shell-<key>` not found on a first visit | 21 | `MISSING_FUNCTIONALITY` | Today a widget is absent until its data lands, which is exactly what slice 02 removes |
| Chart value from the wrong window (quoted below) | 5 | `MISSING_FUNCTIONALITY` | The reported defect and the two pre-existing bugs DESIGN folded into slice 01, reproduced |
| View Data enabled while loading / rating and View Data still shown in a failed frame / `widget-shell-body-<key>` not found | 4 | `MISSING_FUNCTIONALITY` | No pointer block, no header chrome by state, no body hook yet |

The defects, quoted back:

| Specification | What it failed on |
|---|---|
| shows the second window when the first window's answer arrives last | `Work Items Completed: 90` where `…: 7` was expected — the reported "old window lands last and stays" |
| a stretch chosen for a window the reader has left never shows its numbers | `scope 7, 30 days ending 0 days ago` after the reader moved to 90 days — the unguarded Cumulative State Time scope fetch |
| a choice of Work Items answered late for the window the reader left never replaces the current one | `Work Items 11+12, 30 days ending 0 days ago` — the selection is never refetched for the new window |
| the picker offers the new window's Work Items once the window changes | `ITEM-11 (30 days ending 0 days ago)` — candidates are fetched once per mount |
| the filtered series of a window the reader has left never replaces the current one | filtered total `3` (old window) where `9` was expected — the Throughput run chart's filtered series |

## Pins (pass today, run in CI from now on)

| Specification | File | Why it passes today, and why it stays |
|---|---|---|
| a frame left at its defaults shows its chart and header as before | `WidgetShell.status.test.tsx` | Every caller that passes no status must keep today's frame |
| keeps the title, rating and trend at full strength | same | Vacuous today (loading is not drawn); guards the header against being dimmed with the chart |
| still explains the chart when the reader asks | same | Same; the info popover must stay usable while the chart is loading |
| with the filter off, a new window shows the dashboard's own series for it | `ThroughputRunChartCard.loading.test.tsx` | The unfiltered series comes from the page; no extra request may appear |
| an empty answer reads as nothing recorded yet, never as a spinner (×2) | `OverTimeWidgets.loading.test.tsx` | The over-time empty state must survive the new spinner |
| Estimation vs. Cycle Time stays away on a window change once it is known not to be set up | `BaseMetricsView.loading.test.tsx` | Vacuous today (the frame is never there); guards slice 02 against a frame that flickers back on every window change |

## Suite state at hand-off

Committed with every pending specification skipped: `pnpm test` → `Test Files 444 passed | 1 skipped (445)`,
`Tests 6562 passed | 146 skipped (6708)`. `pnpm build` is recorded in feature-delta.md under
`DISTILL / [REF] Pre-requisites`.
Re-run this gate by copying a file, replacing `it.skip` with `it`, and running the copy with
`pnpm vitest run <copy>`; delete the copy afterwards.
