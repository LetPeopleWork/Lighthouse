# Slice 01 — Story 6300 — A Jira Portfolio reads dependencies from a named one-way link phrase

**ADO**: Story #6300 (Epic #6303) · **User story**: US-01 · **Estimate**: ~1 day · **Depends on**: nothing (first slice)
**Release point**: not this slice — #6300 releases after slice 02. This slice alone leaves `main` releasable,
but a mistyped phrase still reads nothing and says nothing until slice 02 (L8).
**Locked decisions**: D1 (any Additional Field, resolved per tracker; field first), D2 (one-way only), D3 (one, replaces the default), D5

## Goal
A Portfolio whose Dependency Field names an Additional Field that resolved to a Jira link type reads each
Feature's dependencies from links of that type, on the end the typed phrase names.

## Why this is thin
The Jira resolver, the direction rule and the link walker all exist from Epic 6028
(`JiraWorkTrackingConnector.cs:1993-2019`, `:2058-2070`; `IssueExtensions.cs:106-120`, `:190-212`). Today
the link-type case falls through to an empty field value (`:1733`) and the selection replaces the default
(`DependencySourceSelector.cs:38-50`), so the result is an empty list. This slice gives that case a reader.

## IN
- `DependencySourceSelector` learns a third source: "a link type the Portfolio named" (beside tracker link and
  field). The tracker supplies what it read; the selector decides — no tracker decides alone. Slice 04 reuses it
  for Azure DevOps.
- Jira: for a link-type Dependency Field, collect the counterpart key on the named end of each matching link.
  A Dependency Field that resolves to an ordinary field is read exactly as today (field lookup first).
- Type name or a phrase both ends share → no dependencies, one warning per refresh naming both phrases (D2),
  shown on the Portfolio's row in Refresh History (D5a) and logged as one line.
- Unit tests over `TrackerWireFormats` fixtures: both directions, rename case, case-insensitivity, both-ways
  refusal, field-valued Dependency Field unchanged, no Dependency Field unchanged.

## OUT
- The "nothing matched" warning for a named link (slice 02).
- Azure DevOps (Story #6312, slices 03–04). Linear / CSV (not applicable, L4).
- Any new UI. The caption under Dependency Field is updated in the finalize docs pass if DISTILL says so.

## Learning hypothesis
Disproves "a one-way phrase is enough to read the direction of a wait" if, on real data, a configured phrase
reads waits backwards for some Features (e.g. links created from the "other" side read differently). Jira
serving every link from both ends (the 6028 spike) predicts it will not.

## Acceptance criteria
AC-1.1 – AC-1.7 in `feature-delta.md`.

## Production data / dogfood
On the dev instance (`:5169`, real Jira history) or the test Jira Cloud site: one Portfolio whose Features
carry a non-Blocks link type (pre-requisite P2 — create a "Dependency" link on a handful of Features if none
exists). Measures `OUT-6303-dependencies-recovered` (AC-1.7): count the Features carrying ≥ 1 link of the named
type to a Feature Lighthouse stores (the tracker side) against the Features whose `DependsOnReferences` hold that
counterpart (the Lighthouse side), before and after. Baseline 0%; target 100%, allowing only counterparts outside
every Portfolio. Record both numbers here.

**Result (2026-10-17, maintainer's live review on the demo Jira Cloud site).** A "Dependency" link type
(`depends on` / `is depended on by`) was added, and LGHTHSDMO-1 depends on -2 and -3 depends on -4 and -5, with
-6 relates to -2 as a link that should not match. Portfolio "Demo Dependencies" (Epics 1–10) and a Team over
their children. Tracker side: 2 Features carry 3 `depends on` links to stored Features. Lighthouse side before
(no Dependency Field): 0 of 3, which is **0%**. After naming `depends on`: 3 of 3, which is **100%**, target
met. All other cases matched too: `is depended on by` flips the direction; `blocks` reads Jira's stock Blocks
from the `blocks` end; no Dependency Field still reads `is blocked by`; naming the type `Dependency` reads
nothing and Refresh History shows the both-ways finding once; switching back clears it.

## Request budget
No additional Jira request on the fetch path; assert the request count against the pre-change baseline.
Live: refreshes with and without a named link took the same time (28–30 s per full download).

## Changed assumptions
- The both-ways finding listed the two phrases in the order they were read ("is depended on by" or
  "depends on"), not outward first as the approved copy shows. Fixed in slice 02: outward phrase first, always.
- The log line for a finding showed code names (`DependencyField`, `NamedLinkPointsBothWays`). Rewritten in
  slice 02 as a plain sentence an admin reads.
- Findings appear in Refresh History only with Entity set to the Portfolio, not the Portfolio's Forecast run.
  That is as designed, but easy to miss when testing by hand.
