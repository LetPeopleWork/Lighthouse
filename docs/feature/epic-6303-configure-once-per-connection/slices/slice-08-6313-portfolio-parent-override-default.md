# Slice 08 — Story 6313 — The Portfolios' Parent Override Field is set once

**ADO**: Story #6313 (Epic #6303) · **User story**: US-07 · **Estimate**: ~0.5 day · **Depends on**: finished #6304 (05a – 06, D19)
**Release point**: no — #6313 releases after slice 12, its last slice (L8). Alone, this slice completes one setting
and leaves `main` releasable.
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
Dev instance's Portfolios on the Jira connection; the reporting customer's configuration when shared.

## Acceptance criteria
AC-7.1 – AC-7.5; AC-F.1 – AC-F.5 (save dialog count and log entry); AC-4.4 and AC-4.8 – AC-4.10 at Portfolio grain.

## Release notes (finalize)
"Portfolios whose Parent Override Field pointed at an Additional Field that had been deleted now read the tracker's
own value again, from their next update."

## Changed assumptions
- 2026-10-16 (DESIGN, maintainer P-5, P-12, UI-4): save dialog count and log entry instead of a save notice;
  deleted-field values repaired at the upgrade; slice 07's list no longer exists.

## Carried from slice 06 review
Two latent issues in slice 06's update question, harmless while only one default with followers is on the wire, that
this slice and slices 09–11 must close before they ship a second one:

1. **Owners following two changed defaults are counted twice.** The question's count sums the followers of each
   changed default, so a Portfolio following both a changed Dependency and a changed Size Estimate default is counted
   once per default. Once a second default with followers exists, `usedBy` must let the page count distinct owners
   across the changed defaults (for example a per-owner list of the defaults each follows, or counts the server works
   out for the set of changed defaults). Test to add: "3 Portfolios follow both the Dependency and Size Estimate
   defaults, both changed → *Update 0 Teams and 3 Portfolios?*".
2. **The body sentence fits only Parent Override.** "Their parents change only once they have been updated." is true
   of a Parent Override default alone. Dependency, Size Estimate and Feature Owner each need their own sentence; that
   is new copy, so it gets a maintainer sketch at that slice's DISTILL (or DELIVER, if DISTILL left it open).

Also: each new default states on `WhereEachDefaultSits` (in `ModifyConnectionSettings.tsx`) when it is read —
`readWhen: "updating"` counts towards the update question, `"pageLoads"` (the Estimation defaults) does not.

## Maintainer decision 2026-10-18 — the "Connection default" entry shows even when the default names nothing
Found in #6304's manual review: an owner following a default that names nothing showed no *Connection default* entry
and no helper text, so it looked exactly like an owner that chose *None*, and once it picked *None* there was no way
back to following until a default was set. Decided (option B): on a tracker that honours the default (Jira, Azure
DevOps), the first entry is always offered — `Connection default (None)` when the default names nothing,
`Connection default (<value>)` otherwise — and the helper text "Set on the <tracker> connection." shows whenever the
owner follows. Do it in this slice, for Teams (`AdvancedInputs.tsx`) and Portfolios alike: "follows" means only
`parentOverrideSource === "ConnectionDefault"`, no longer "and the default names a field". Frontend only; the server
already accepts *Connection default* with no default set. Flip the test that pins "no entry without a default" and add
one for picking the entry to switch back from *None*.

## Maintainer decision 2026-10-18 — the update question's body sentence per default
Approved copy for slices 08–11 (configurable terms render in the instance's words; the closing "Save only: they change
at their next scheduled update." stays as it is):

| Changed default | Sentence after "N <owners> follow the … default." |
|---|---|
| Parent Override Field (Teams or Portfolios) | Their parents change only once they have been updated. |
| Dependency Field | Their dependencies change only once they have been updated. |
| Size Estimate Field | Their Features' sizes change only once they have been updated. |
| Feature Owner Field | Their Features' owners change only once they have been updated. |
| Several defaults changed at once | What they read changes only once they have been updated. |
