# Slice 01: The query commits on blur

**Goal:** the Team/Portfolio query field saves once, when the user leaves it, and not while they type.

**Story:** US-01 (ADO #6456).

## IN
- A local draft inside the query `TextField` in `GeneralSettingsComponent`, committed through
  `onSettingsChange` on blur when it differs from the committed value.
- A value written from outside (board wizard, settings load or reload) replaces the draft.
- A read-only field never commits.
- Vitest + RTL tests for AC 1-7.

## OUT
- Any other field's timing; `beforeunload`; any visible "unsaved" signal; the create wizards; the backend.

## Learning hypothesis
Disproves *"commit on blur is enough"* if edits are routinely lost to tab closes mid-edit.
Confirms it if one Tab-out produces exactly one save and refresh on a real connection.

## Acceptance criteria
AC 1-7 in `feature-delta.md` → US-01.

## Dependencies
None.

## Effort
A few hours. Reference class: Bug 5628 settings-autosave fix (same hook, same component family).

## Spike
None needed.
