# Slice 02: The save says what it started

**Goal:** after a connection save that queued updates, the System Admin is told how many Teams and Portfolios are
being updated and can reach the Task Manager; a save that queued nothing says nothing about updates.

## IN scope
- The save reports what it actually queued (counts per kind; names if the DISTILL sketch wants them, UI-3).
- A notice next to Save (or the dialog's confirmation, D6) with those counts in configured Terminology, and a way
  to the Task Manager (AC-2.1 … AC-2.4).
- The message survives whatever happens after Save (stay on the page or navigate, Q5, UI-5) (AC-2.5).
- If the message fetches anything of its own: loading and could-not-load looks from DESIGN.

## OUT of scope
- Save without updating (slice 03).
- Per-owner progress on the connection page; the Task Manager already shows it.

## Learning hypothesis
- **Disproves** "being told the count is enough to trust the field" if, dogfooding, the maintainer still opens a
  Team to check the field before building the rule.
- **Confirms** it if he goes from the message to the Task Manager and then straight to the rule.

## Acceptance criteria
US-02, AC-2.1 … AC-2.5 in `../feature-delta.md`.

## Dependencies
Slice 01 (the queued set it reports). The DISTILL sketch (UI-1, UI-3, UI-5).

## Effort
About 5 hours: an additive field on the save response (extend the test builder first), the message, Terminology
plurals, tests. Reference class: the save-state indicator on Team settings.

## Dogfood
Dev instance: add a field and read the message; rotate the token and see none. One demo-data check with *Team*
renamed under Terminology.
