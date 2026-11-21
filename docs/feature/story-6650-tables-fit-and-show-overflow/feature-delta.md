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
| 7 | 2026-11-21 | **Overview at phone width:** when the table is too narrow for the four action icons (Details, Edit, Clone, Delete) beside a readable Name, they collapse into one pinned "⋮" menu button (about 48 px) that opens Details, Edit, Clone and Delete (only the actions the person may use). Sketch approved as shown: `Name ▸ … Features │ ⋮` per row, menu listing the four actions. Found by the completion pass: at 375 px the pinned 200 px actions column covered the Name (grid 269 px). |

Open for the S2 acceptance designer, within the sketch: the exact screen-reader wording of the overflow text (for example
"More columns to the right"). Loading and failure looks are unchanged: no new data is fetched.

S2 outcome of that question: the grid's description reads "This table scrolls sideways. More columns to the right."
(or "to the left", "on both sides") and is absent when nothing is hidden. The cue itself is a DOM overlay element per
edge (`aria-hidden`, no pointer events), as the design's "edge overlays" says, not a CSS shadow on the grid, so tests
may count it.
