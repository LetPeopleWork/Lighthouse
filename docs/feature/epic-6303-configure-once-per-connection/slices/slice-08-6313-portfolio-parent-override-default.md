# Slice 08 — Story 6313 — The Portfolios' Parent Override Field is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-07 · **Estimate**: ~0.5 day · **Depends on**: finished #6304 (slices 05a–07, D19)
**Release point**: yes — each #6313 slice completes one setting and can be released on its own (L8).
**Locked decisions**: D8 (separate from the Teams' default), D9–D13, D21

## Goal
A separate "Parent Override Field for Portfolios" default on the connection, followed by Portfolios in
*Connection default* mode, exactly as slice 05a does for Teams. Saving it shows the save notice naming the
Portfolios that follow.

## Why separate
A Team's parent link (Work Item → Feature) and a Portfolio's (Feature → the item its parent link names) are
different links (Epic 6028 D7). One shared default would make every Portfolio override it.

## IN
- Second default column; Portfolio mode column (D10 back-fill); new Portfolios start on *Connection default*
  (D11); Portfolio Advanced dropdown entry; the followed name on the Portfolio settings DTO.
- Reused unchanged from #6304: slice 05a's resolver and save notice, slice 05b's delete refusal, slice 06's queue,
  slice 07's list.
- The Portfolio PUT gets slice 05b's G-R1 (b) rule for the first time (05b covered Teams): a PUT without the mode
  keeps the stored mode, unless it sends a present, non-empty Parent Override value that differs from the effective
  one — then *Own* with that value; an empty or absent value changes nothing. The GET-then-PUT round trip passes in
  both the echoing and the property-dropping shape (AC-4.9, AC-4.10 at Portfolio grain).
- Shown on Jira and Azure DevOps connections only (D21).

## OUT
- Anything new in the resolver, save notice or queue.

## Learning hypothesis
Disproves "Portfolios on a connection share their parent link" if dogfood Portfolios mostly need different ones —
then few follow the default.

## Production data / dogfood
Dev instance's Portfolios on the Jira connection; Steve's customer's configuration when shared.

## Acceptance criteria
AC-7.1 – AC-7.4; AC-F.1 – AC-F.5; AC-4.8 – AC-4.10 at Portfolio grain.

## Changed assumptions
(filled during DELIVER)
