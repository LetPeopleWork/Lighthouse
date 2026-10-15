# Slice 03 (conditional): Save now, update later

**Ships only if the DISTILL sketch session picks the dialog form of D6.** Otherwise this slice is dropped and
recorded as such in `../feature-delta.md`.

**Goal:** when saving would start updates, the System Admin can save without starting them; the field is stored,
nothing is queued, and the copy says each Team and Portfolio loads the field on its next scheduled update.

## IN scope
- The dialog appears only when saving would queue at least one update; "update now" is the default (D2, AC-3.4).
- "Save without updating" stores the connection exactly like a normal save and queues nothing (AC-3.1).
- Its copy names the next scheduled update as when the field loads (AC-3.2).
- A guard that the next scheduled update of such an owner downloads everything (AC-3.3; already true through the
  fingerprint).
- If knowing "would queue" before Save needs a fetch: loading and could-not-load looks from DESIGN.

## OUT of scope
- Picking a time, or deferring to a quiet hour. "Later" means the existing schedule.
- A threshold that decides for the admin (D5).

## Learning hypothesis
- **Disproves** "admins want to choose the moment" if the "later" choice is taken on (almost) no field saves
  (usage-data event, if DEVOPS adds one).
- **Confirms** it if large-instance admins use it on a noticeable share of saves.

## Acceptance criteria
US-03, AC-3.1 … AC-3.4 in `../feature-delta.md`.

## Dependencies
Slices 01 and 02. DISTILL sketch UI-1 and UI-4.

## Effort
About 3 hours: the choice on the save, the dialog on an existing confirmation control, copy, tests.
Reference class: the existing delete-confirmation dialogs.

## Dogfood
Dev instance: save a field with "later", confirm the Task Manager stays empty, then trigger the scheduled run (or
wait) and check the field arrives.
