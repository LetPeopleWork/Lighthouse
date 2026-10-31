# ADR-267: Several reports print as one document from the same hidden frame, ready only when every report is

**Status**: Accepted, built on main 2026-10-31 (not yet released). Proposed (DESIGN, 2026-10-27, PROPOSE mode). The ceiling N = 50 was LOCKED by the maintainer on
2026-10-27; it drops to 25 if a 50-report print fails in the Linux desktop app.
**Feature**: `epic-6350-bulk-reports` (Epic #6350, slice 07)
**Relies on**: ADR-245 (the print page and its read-only port), ADR-246 (bundled typeface and drawn marks), ADR-255
(Export PDF prints from a hidden same-origin frame the app draws into).

## Context

A lead selects several reports on the Reports page and wants one PDF, each report on its own pages exactly as its own
PDF shows it, in the table's order. If any selected report cannot be read, no PDF is made; the page names it and offers
Retry or "Leave it out". The macOS desktop app cannot print (Bug 6425). Single export already prints from a hidden
frame through a pure page-state function, with a 30-second budget.

## Decision

1. The selection's Export PDF mounts the **same hidden print frame**, which draws one print content per selected
   report, in the table's order, each starting on a new page with its own header and footer.
2. Each report's content works out its own state as today; a pure function rolls the states up: ready only when every
   report is ready; any failed report means no print, and a dialog titled "No PDF was made" names which report failed, with
   [Cancel] [Leave it out] [Retry]; Leave it out deselects it and prints the rest, and is not offered when it would
   empty the selection. No snackbar. A failed shared part (fonts, terms, the mark) fails the whole document as today.
3. The reports are read in parallel through the app's query cache; the browser limits connections per host. The
   frame's budget grows with the number of reports (a starting value of 30 s plus 2 s per report beyond the first,
   measured in delivery).
4. A selection above the shared ceiling N = 50 is refused before anything is read.
5. A `/print/reports?reports=…` page mirrors the frame for the end-to-end print check and for a pasted link; it never
   prints by itself.
6. The macOS desktop app shows the action disabled with the single export's reason.

## Alternatives considered

- **One PDF per report, merged elsewhere.** No new code, and exactly the chore the Epic removes. Rejected.
- **Print what could be read and list the missing reports on a last page.** A file that reads complete but is not;
  the maintainer chose no PDF instead. Rejected.
- **Read the reports one after another.** Gentler on the server, but a 50-report export would wait for the sum of
  every read. Rejected; the browser's own connection limit already bounds the parallel reads.
- **Render the PDF on the server.** Belongs to the scheduled-sending work (#5882). Out of scope.

## Consequences

- Positive: one report in the combined PDF reads exactly as its own PDF at the same moment.
- Positive: single export is unchanged; the roll-up is a pure function with a state table.
- Negative: a large selection holds many reports' charts in one frame; N bounds it and delivery measures it in the
  Linux desktop app.
