# Slice 02 — Story 6300 — A named link that matched nothing says so (Jira)

**ADO**: Story #6300 (Epic #6303) · **User story**: US-02 · **Estimate**: ~0.5 day · **Depends on**: slice 01
**Release point**: yes — #6300's last slice; #6300 releases after it (L8).
**Locked decisions**: D5 (no match warns, never falls back), D5a (findings on Refresh History)

## Goal
When a Portfolio names a link type and no Feature in the whole-query fetch carries a matching link, one
warning per refresh names what was looked for and the phrases that were seen.

## Why
`ReportLinksThatMeantNothingHere` (`JiraWorkTrackingConnector.cs:1275-1309`) returns early as soon as a
Dependency Field is set (`:1282`) — exactly when an admin is setting one up. After slice 01 that early return
hides the named-link case too.

## IN
- Generalise the existing diagnostic: expected name = the named phrase when a link type is named, `is blocked by`
  otherwise. Still silent when a field (not a link type) is named.
- Same rules as today: whole-query fetch only, silent when anything matched, silent when no links at all, one
  line per refresh.
- The warning appears on the Portfolio's row in **Refresh History** (Settings → System Info), the surface Story
  6301 uses for a refresh's per-owner findings (D5a), and in the log as one line. DESIGN shapes the row's
  wording and count; the surface is fixed.
- Docs prose drafted for `docs/portfolios/edit.md` §Dependency Field and `docs/concepts/worktrackingsystems/jira.md`
  (:266-271) — published at feature finalization.

## OUT
- Azure DevOps's equivalent (Story #6312, slices 03–04, same surface).

## Learning hypothesis
Disproves "an admin can fix a mistyped phrase from the warning alone" if the dogfood admin still has to open
Jira to find the right phrase.

## Acceptance criteria
AC-2.1 – AC-2.4.

## Production data / dogfood
Same Portfolio as slice 01, deliberately mis-named (`depend on`): read the warning, correct, re-run.

## Changed assumptions
(filled during DELIVER)
