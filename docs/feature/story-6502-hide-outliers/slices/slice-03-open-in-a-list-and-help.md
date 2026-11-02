# Slice 03: Open in a list, the ⓘ sentence and the docs passage

Story: US-6502-01 (second half). Builds on slice 01. (Renumbered from the former slice 04 after the merge of slices 01
and 02, AFK-6.)

## Goal

A reader who wants sorting, export or more than the first 10 outliers opens the same dialog a dot opens, and the
widget's own help explains the chip in the reader's words.

## IN scope

- "Open in a list" at the foot of the popover opens the existing `WorkItemsDialog` (`closedItems` context, a
  `DataGridBase`) with the outliers of the visible types. Same columns, sort, export and tracker links as when a
  dot is opened. The dialog title uses the reader's terms, for example "{Work Items} with a {Cycle Time} more than 5
  times the median". The dialog count equals the chip count (the dialog is not capped at 10 rows).
- Focus: the popover closes and focus moves to the dialog; when the dialog closes, focus returns to the chip if it is
  still mounted, otherwise to the widget heading.
- ⓘ text of the `cycleScatter` widget gains one sentence naming the chip and the rule, written with the
  Terminology words (`widgetInfoMetadata.ts` takes `terms`; `WidgetInfoTerms` gains `cycleTime`; built for Teams and
  Portfolios). Learn More stays `#cycle-time-scatterplot`.
- Docs passage "how to read it" at that anchor (written at finalize, per the per-feature docs rule).

## OUT of scope

A new column or marker in View Data (decision Q9: unchanged). A new grid.

## Learning hypothesis

Disproves: "the compact list is enough; nobody needs the full dialog." If a reader opens the dialog on every popover
visit, the popover list is the wrong size and the dialog should become the first step. Confirms the reuse: no new
table component is needed.

## Acceptance criteria (summary)

- The link opens the dialog with exactly the chip's outliers; closing returns focus as above.
- The ⓘ sentence and the dialog title carry the reader's configured terms (a renamed Work Item / Cycle Time shows).

## Dependencies

Slice 01.

## Effort and reference class

About 0.5 day for the code, plus the docs passage at finalize. Reference class: other widgets that open
`WorkItemsDialog` from a control. Production data: dev instance.
