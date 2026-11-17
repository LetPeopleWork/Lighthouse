# Story 6650 — tables fit their width and show when columns are off screen

DISCUSS and DESIGN are in `brief.md` and `architecture.md` (nwdes renders them from `handoff.json`). This file holds
the DEVOPS answer and the maintainer decisions taken at the start of DELIVER.

## DEVOPS

No platform risk: frontend only, no new dependency (the community `@mui/x-data-grid` stays), no API, no migration, no
setting. The stored grid state in the browser keeps its shape.

**Usage data: N/A** (maintainer, 2026-11-17). A layout fix in a shared component; the existing report and delete events
already show whether people act on a table.

## Maintainer decisions

| # | Date | Decision |
|---|---|---|
| 1 | 2026-11-15 | Both: columns fit the width better (shrink, long text truncates with a tooltip), and a visible cue shows when columns still do not fit. |
| 2 | 2026-11-17 | The actions column is also pinned to the right edge on every shared table that has one. |
| 3 | 2026-11-17 | Phone width: the table scrolls sideways inside its box with the cue; no card-per-row layout. |
| 4 | 2026-11-17 | **Sketch approved as sketched** (`Name ≥ 160 px`, other text columns `≥ 120 px`; numbers, dates and states never cut; full text in a tooltip on hover and keyboard focus; a width the person set is kept, Reset returns to the fitted layout). |
| 5 | 2026-11-17 | **Cue style: a soft inner shadow only**, on the side where columns are hidden, no text hint; screen readers still get the overflow text. |
| 6 | 2026-11-17 | **Rigor:** S1 `thorough` (the shared fit and the sticky pinning: Opus crafter and acceptance designer, Sonnet reviewers); S2–S4 `standard` (Sonnet; Haiku reviewers). |

Open for the S2 acceptance designer, within the sketch: the exact screen-reader wording of the overflow text (for example
"More columns to the right"). Loading and failure looks are unchanged: no new data is fetched.
