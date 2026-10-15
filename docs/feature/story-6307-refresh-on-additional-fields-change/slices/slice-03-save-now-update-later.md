# Slice 03: Save now, update later

Confirmed at DISTILL (2026-10-15): the sketch session picked the dialog with an opt-out (S1), so this slice ships.

**Goal:** the question from slice 02 also offers `Save only`. It stores the connection exactly like a normal save,
starts no update, and says each Team and Portfolio fills the field in with its next scheduled update. Usage data
learns which answer was given.

## IN scope
- `Save only` in the question, and its body sentence "Save only: they fill in with the next scheduled update
  instead." (S1, S4).
- `Save only` saves with `?startUpdates=false` and returns to the overview (S6). The server side is slice 01's.
- Usage-data event `WorkTrackingSystemFieldsChanged` (in this slice, decided in AFK, revisit), appended to `UsageDataEventName` after `ReportDeleted`,
  carrying only `updates: Now | Later`. Sent once when the question is answered and the save went through:
  `Save and update` is `Now`, `Save only` is `Later`. Never on `Cancel`, never when no question was asked, never on a
  refused save (S7). Listed in `docs/settings/usagedata.md`, with the "other N events" counts bumped.

## OUT of scope
- Picking a time, or deferring to a quiet hour. "Later" means the existing schedule.
- A threshold that decides for the admin (D5).
- `lh`/MCP offering the choice (DO-3).

## Learning hypothesis
- **Disproves** "admins want to choose the moment" if `Later` is the answer on (almost) no field saves
  (`OUT-6307-opt-out-rate`, under 10 % suggests removing the choice).
- **Confirms** it if large-instance admins use it on a noticeable share of saves.

## Why not merged into slice 02
Slice 02 alone already gives value: the admin learns before saving how many updates the save starts, and can back
out. Slice 03 tests a different hypothesis (that the moment matters) and owns the only reason the event carries a
property. Keeping it apart keeps slice 02's hypothesis readable on its own.

## Acceptance tests
`EditConnection.fieldChange.test.tsx`, describe "slice 03" (6 pending, 1 guard green now); one pending test in
`WorkTrackingSystemService.test.ts`; `WorkTrackingSystemFieldsChangedUsageEventTests` (12 pending, backend). US-03
AC-3.1 to AC-3.4 at the page.

## Dependencies
Slices 01 and 02.

## Effort
About 3 hours: the third button and its sentence, the choice on the service call, the event on both stacks, the
usage-data page, tests. Reference class: `OptionalFeatureToggled`, the other event with a closed property.

## Dogfood
Dev instance: save a field with `Save only`, confirm the Task Manager stays empty, then trigger the scheduled run (or
wait) and check the field arrives. With usage data switched on, check one event per answered question.
