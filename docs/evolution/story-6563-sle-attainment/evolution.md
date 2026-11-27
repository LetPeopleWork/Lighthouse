# Story 6563 — see whether we keep our SLE

Delivered 2026-11-27. ADO #6563 "Make the SLE easier to find and compare with actual performance" (no parent Epic).
Workspace: `docs/feature/story-6563-sle-attainment/`. Route: spine waves, then nwdes at DELIVER. Size L.

## What was delivered

Two free views of one question, "do we keep our SLE?", for Teams and Portfolios.

- **Within SLE widget** on the Flow Overview. The share of the Work Items closed in the selected range that finished
  within the SLE's days, for example "72.7 % (8 / 11)", then "of Work Items closed within 10 days or less", then
  "Target 70 %". The status uses the Cycle Time Percentiles rule (Sustain, Observe, Act). The header carries the trend
  against the previous period. No SLE set reads "No SLE set" with Act; fewer than 5 closed Work Items reads "too few
  to judge" with no verdict. View Data lists the closed Work Items, longest first.
- **Within SLE Over Time chart** in Predictability. A daily record per owner, trailing window (30, 60 or 90 days) and
  day, each day with the SLE in force that day (ADR-003). A 30/60/90 day toggle, a fixed 0 to 100 axis, a stepped target
  line, and gaps instead of 0 for days with too few or no record. Past days are filled in from history against today's
  SLE and drawn as open markers, with the note "○ filled in from history (measured against today's SLE)".
- **Usage events** `WithinSleListOpened` (31) and `WithinSleHorizonSwitched` (32), name-only.

Context map: `WithinSleShare` (Domain) decides the share and the minimum of 5 for both views;
`WithinSleSnapshotWriter` is the only writer of `WithinSleSnapshot`; `WithinSleRecordingHandler` records on the refresh
events. ADR-270's Flow metrics glossary has the rows "Share within the SLE", "SLE in force on a day" and "Within SLE
daily record".

## Commits

48 commits in `git log --oneline 6b6fc630d..d5a24e61b`. The key ones:

| Commit | What |
|---|---|
| `06c1ebcde`, `c228a0c02`, `878cc697c`, `d47b3fe45`, `16b4e8072` | DISCOVER, DIVERGE, DISCUSS with the sketch review, DESIGN, DEVOPS |
| `1d79de30e` | examiner expectations E1 to E15 |
| `1f410eb73` | S5, the Within SLE widget shows the share, count and target |
| `3bf1c29b1`, `71edbf191` | S6, status, trend and the empty states |
| `788493154` | S7, the daily record and its chart in Predictability |
| `380a2ef8f` | S8, past days filled in from history and marked on the chart |
| `539e660ba`, `a88875122`, `ff96941d4` | fixes from the whole-candidate review |
| `d2a1d02a8` | whole-candidate re-review and EXAMINE, accepted |
| `885f02848` | mutation gate, backend 86.59 %, frontend 81.40 % |
| `e321f5895`, `a4e14de82`, `d5a24e61b` | rule-family and composition tests, code follows the published contracts |

The slice reviews and oracle reviews are the `docs(metrics): story 6563 S* ...` commits in the same range.

## Gates

- Mutation: backend 86.59 %, frontend 81.40 % (minimum 80 %), `docs/feature/story-6563-sle-attainment/mutation/results.md`.
- Whole-candidate review and EXAMINE: accepted (`d2a1d02a8`).

## Knowingly left open

- `minimumClosed` is not yet required on the frontend types, because the oracle fixtures do not carry it.
- An oracle test holds a duplicated render helper.
- The View Data list may include Work Items with a Cycle Time of 0 that the share excludes, so its row count can
  exceed the closed count in the share.
- The chart has no screen-reader data alternative.
- EXAMINE expectations E1, E2, E12, E13 and E15 were waived and covered by the maintainer's manual review.
- The share in the widget body is drawn in the theme's primary colour, while the sketch review asked for it to take
  the status colour (green, amber, red). The status chip in the header carries the status.
- The demo Portfolio has no SLE, so its Within SLE widget shows "No SLE set" with Act. The demo Teams have an SLE
  (85 % within 7 days) and show a share.
- The Within SLE Over Time screenshot shows only the days the fill worked out on that visit (a bounded number per
  visit), so the line has gaps.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done. `docs/metrics/flow-overview.md` section "Within SLE" (`#within-sle`), `docs/metrics/predictability.md` section "Within SLE Over Time" (`#within-sle-over-time`), and the widget lists in `docs/metrics/widgets.md`. Both anchors match the Learn More URLs in `widgetInfoMetadata.ts`. Written with the configurable terms (Work Item, SLE, Team, Portfolio). |
| Screenshots | Done. `@screenshot` tests "Take @screenshot of the Within SLE widget on the Flow Overview" and "Take @screenshot of the Within SLE Over Time chart in Predictability" in `Lighthouse.EndToEndTests/tests/specs/screenshots/Screenshots.spec.ts`, through the page objects `WithinSleWidget` and `WithinSleOverTimeWidget`. Images `docs/assets/features/metrics/withinSle.png` and `withinSleOverTime.png`. |
| Demo data | Checked, no change. The demo Teams carry an SLE of 85 % within 7 days and show a share (19 of 32 closed Work Items for Team Zenith). The demo Portfolio has no SLE, so its widget shows "No SLE set"; left as it is. |
| Website assets | Deferred by the maintainer: decided at /release. |
| Clients (CLI, MCP) | Deferred by the maintainer: no follow-up for now. |
| Context map, `ARCHITECTURE.md` | Done, no drift found after the rebase onto Story 6382's contract moves. ADR-270 (glossary rows, "Snapshot day" with `RecordedAt` in four tables) and `ARCHITECTURE.md` section 6 (`WithinSleSnapshot` row, ADR-003) match the code. |
| Usage data | Done. Events 31 and 32 are in `docs/settings/usagedata.md`. |
| Mutation | Done. Backend 86.59 %, frontend 81.40 %, `docs/feature/story-6563-sle-attainment/mutation/results.md`. |
| Release notes | N/A; written at /release. |
