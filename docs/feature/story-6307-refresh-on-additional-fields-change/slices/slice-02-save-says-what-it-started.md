# Slice 02: The page asks before it updates the Teams and Portfolios

Reshaped at DISTILL (2026-10-15) after the sketch session (S1 to S5). Was "The save says what it started"; the
after-save notice is gone (S3) and the question before the save takes its place. The file name is kept so earlier
references still resolve.

**Goal:** when Priya presses Save on an existing connection whose Additional Fields she changed, and at least one
Team or Portfolio uses it, the page asks `Update {n} {Teams} and {m} {Portfolios}?` before saving, in this instance's
words, with `Cancel` and `Save and update` (the default). Answering `Save and update` saves as today and returns to
the overview, where the Task Manager shows the updates slice 01 started.

## IN scope
- `usedBy { teams, portfolios }` on every connection in `GET /api/latest/worktrackingsystemconnections`, the read the
  page already makes, through a read-only connection-usage port (DDD-17, S4). A connection only Portfolios read from
  answers 0 and 2. The frontend service keeps it.
- The question, modelled on `DeleteConfirmationDialog`: title, the first body sentence, `Cancel`, `Save and update`
  focused. Singular and plural from Terminology, nothing literal (S4).
- Asked only after the connection passed validation, only on an existing connection, only when the field list
  differs from the one loaded (an add that was cancelled, or added and removed again, is no change), and only when
  the connection has at least one Team or Portfolio (S5).
- `Cancel` saves nothing and leaves the page and its edits as they were (S6). A save refused after the question
  (someone else saved first) shows the existing message and stays on the page.
- Remove the "Additional Fields will be loaded with the next refresh of data." alert (S2).
- No new loading or failure look: the page already shows its own until the list read lands (S4).

## OUT of scope
- `Save only` and the second body sentence (slice 03).
- The usage-data event (slice 03, decided in AFK, revisit).
- Any notice after the save (S3).

## Learning hypothesis
- **Disproves** "being told the count before saving is enough to trust the field" if, dogfooding, the maintainer
  still opens a Team to check the field before building the rule.
- **Confirms** it if he goes from the question to the Task Manager and then straight to the rule.

## Acceptance tests
`Slice02ConnectionsSayWhoUsesThemTest` (4 pending, backend); `EditConnection.fieldChange.test.tsx`, describe
"slice 02" (14 pending, walking skeleton first; 7 guards green now, one of them the new-connection path); one pending test in
`WorkTrackingSystemService.test.ts`. US-02 as reshaped (S1 to S5), AC-1.8 as replaced by S2.

## Dependencies
Slice 01 (without it, `Save and update` would start nothing). Sketch decisions S1 to S6.

## Effort
About 4 hours: the count query on the list (extend the DTO builder first), the service field, the dialog, the
"fields changed" comparison, Terminology plurals, tests. Reference class: the delete-confirmation dialogs.

## Dogfood
Dev instance: add a field and read the question; rotate the token and get none. One check with *Team* renamed under
Terminology. Demo data cannot show it: its only connection is CSV, which has no Additional Fields.
