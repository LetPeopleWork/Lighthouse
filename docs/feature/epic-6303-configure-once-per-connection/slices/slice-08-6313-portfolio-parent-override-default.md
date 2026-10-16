# Slice 08 — Story 6313 — The Portfolios' Parent Override Field is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-07 · **Estimate**: ~0.5 day · **Depends on**: finished #6304 (05a – 06, D19)
**Release point**: yes — each #6313 slice completes one setting and can be released on its own (L8).
**Locked decisions**: D8 (separate from the Teams' default), D9–D13, D21

## Goal
A separate "Parent Override Field for Portfolios" default on the connection, followed by Portfolios in
*Connection default* mode, exactly as slice 05a does for Teams. Saving it shows the save dialog count and the log
entry naming the Portfolios that follow.

## Why separate
A Team's parent link (Work Item → Feature) and a Portfolio's (Feature → the item its parent link names) are
different links (Epic 6028 D7). One shared default would make every Portfolio override it.

## IN
- Second default column; Portfolio mode column (D10 back-fill as amended: a value pointing at a deleted field becomes
  *Connection default*); new Portfolios start on *Connection default* (D11); Portfolio Advanced dropdown entry
  (UI-2); the followed name on the Portfolio settings DTO.
- Reused unchanged from #6304: slice 05a's resolver, save dialog count and log entry, slice 05b's delete refusal,
  slice 06's queue and update question.
- The Portfolio PUT gets slice 05b's G-R1 (b) rule for the first time (05b covered Teams): a PUT without the mode
  keeps the stored mode, unless it sends a present, non-empty Parent Override value that differs from the effective
  one — then *Own* with that value; an empty or absent value changes nothing. The GET-then-PUT round trip passes in
  both the echoing and the property-dropping shape (AC-4.9, AC-4.10 at Portfolio grain).
- Shown on Jira and Azure DevOps connections only (D21).

## OUT
- Anything new in the resolver, save dialog, log entry or queue.

## Learning hypothesis
Disproves "Portfolios on a connection share their parent link" if dogfood Portfolios mostly need different ones —
then few follow the default.

## Production data / dogfood
Dev instance's Portfolios on the Jira connection; Steve's customer's configuration when shared.

## Acceptance criteria
AC-7.1 – AC-7.5; AC-F.1 – AC-F.5 (save dialog count and log entry); AC-4.4 and AC-4.8 – AC-4.10 at Portfolio grain.

## Release notes (finalize)
"Portfolios whose Parent Override Field pointed at an Additional Field that had been deleted now read the tracker's
own value again, from their next update."

## Changed assumptions
- 2026-10-16 (DESIGN, maintainer P-5, P-12, UI-4): save dialog count and log entry instead of a save notice;
  deleted-field values repaired at the upgrade; slice 07's list no longer exists.
