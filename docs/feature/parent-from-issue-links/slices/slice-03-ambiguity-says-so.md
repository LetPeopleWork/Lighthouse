# Slice 03 — Ambiguity refuses, and names the candidates

**Feature**: parent-from-issue-links · **ADO**: #6031 · **Story**: US-03 · **Estimate**: ~3h
**Reference class**: `ReportLinksThatMeantNothingHere` (`JiraWorkTrackingConnector.cs:1222-1254`) — the
existing pattern for "nothing matched, and here is what we did see", built for dependencies. This slice
is its mirror: too much matched, and here is what.

## Goal

An item with more than one candidate parent takes none, and the administrator is told which candidates
were in play.

## IN scope

- Two or more *distinct* counterpart keys of the configured type → no parent, and a warning naming the
  item and every candidate key (AC-3.1, AC-3.2). Keys, not a count — a count says there is a problem, the
  keys say where to look.
- Duplicate links to the same key collapse to one before counting (AC-3.3). A tracker that records the
  same relationship twice is tidy-in-a-different-way, not ambiguous.
- The refusal is per item and local to it: every other item in the same fetch parents normally and the
  refresh reports success (AC-3.4).
- The warning surfaces where an administrator already looks, not only in the log (AC-3.5). DESIGN picks
  which existing surface; this slice does not add one.

## OUT of scope

- Any tie-break rule. There is no "first wins", no "nearest", no "most recently linked". D4 refuses on
  purpose: a silently wrong parent moves a Work Item into a Feature it does not belong to, corrupts that
  Feature's size, and looks exactly like correct data.
- A per-item override to resolve the ambiguity inside Lighthouse. That would make Lighthouse an author of
  hierarchy (D8). The fix is in the tracker, or in choosing a different link type.

## Learning hypothesis

**Disproves, if it fails**: that ambiguity is rare enough to be an error case rather than the norm.

D4 assumes the configured link type is specific to the hierarchy, so a given item carries at most one of
them. That is an assumption about how people use link types, not about the API, and it can be wrong in an
ordinary way: an instance where "caused by" is also used between sibling items, or where the same link
type carries both the hierarchy and general relatedness, would see most items refuse.

**The measurement**: on the instance slice 02 runs against, count how many fetched items have 2+
candidates of the configured type. Record the number and the denominator here.

**The gate is a number, not a judgement — ≥ 5% of fetched items ambiguous sends the feature back to
DESIGN.** Below 5%, refusing is a safety net and the design stands. At or above it, refusing is the
workflow rather than the exception, and the match needs narrowing beyond the type name — by the
counterpart's work item type, most likely. That is a design change, not a tweak.

The threshold is stated in advance so the result cannot be argued into agreement with the design after
the fact. It is a pre-registered gate, which is the only kind a hypothesis owned by its own author can
honestly have.

**Confirms, if it succeeds**: D4's refusal is a safety net rather than a workflow, and the feature is
finished apart from slice 04's boundary.

### Measurement result — 2026-09-19, `letpeoplework.atlassian.net`, project `LGHTHSDMO`

Read-only, over every issue the project holds, matching the link type the way the resolver does — on
the type's name, inward label or outward label, case-insensitively.

| | |
|---|---|
| Issues fetched (denominator) | **4843** |
| Carrying at least one candidate of the configured type (`Cloners`) | 4 |
| Carrying 2+ distinct candidates — ambiguous | **1** (`LGHTHSDMO-1716`) |
| Share of fetched issues | **0.02%** |

**The gate passes as written: 0.02% is far below 5%, so the feature does not go back to DESIGN.**

**And the measurement cannot bear the weight the hypothesis puts on it. That has to be said plainly
rather than banked.** The one ambiguous issue is the fixture this feature seeded, so the numerator is
our own. Widening to every link type the instance defines, to get away from a type we chose ourselves:

| Link type | Issues carrying it | Ambiguous | Share of carriers | Share of fetched |
|---|---|---|---|---|
| `Cloners` | 4 | 1 | 25.0% | 0.02% |
| `Blocks` | 4 | 2 | 50.0% | 0.04% |

Eight issues out of 4843 carry any link at all. The denominator the gate is written against —
*fetched* items — is therefore dominated by issues that could not be ambiguous under any link type,
and it would keep passing on this instance no matter how ambiguously the links that do exist were
used. Among items that actually carry the type, 1 in 4 and 2 in 4 are ambiguous; on n=4 that number
means nothing either, but it is the number the hypothesis is actually about.

(The two ambiguous `Blocks` carriers are `LGHTHSDMO-7` and `LGHTHSDMO-9`, which the delta already
records as having two counterparts each — an independent cross-check that the counting matches what
was read by hand.)

**So: the pre-registered gate is satisfied, and it is unfalsifiable on this instance.** A demo project
with essentially no link usage cannot tell a safety net from a workflow. The real reading needs an
instance where someone links issues as a matter of habit — the same one
`OUT-PFIL-hierarchy-recovered` is already waiting on. Recorded there rather than closed here.

## Acceptance criteria

AC-3.1 through AC-3.5 in `feature-delta.md`.

## How the count reaches the refresh log (decided during delivery)

The surface AC-3.5 asks for is the refresh log row, which is written by `TeamUpdater` /
`PortfolioUpdater` from the `SyncOutcome` that `WorkItemService` returns. The connector is what reads the
links and knows the answer, so the count has to travel from the adapter to the update pipeline. Two
routes were open:

1. **Widen what the connector hands back.** Taken.
2. **Read an ambient scoped context** from inside the connector call, the way the cancellation token
   already reaches it. **Rejected.** ADR-183 considered exactly this shape under "Ambient only, connector
   port untouched" and turned it down — not on reach, which it concedes, but on layering: it leaves a
   driven adapter depending on update-pipeline state with nothing in the code saying so, and makes the
   adapter untestable except by arranging an ambient. Re-adopting it here would contradict an accepted
   decision.

Route 1 costs no port signature change. `WorkItemBase` already carries a connector-populated,
non-persisted, read-downstream member — `SyncedTransitions` — and the marker follows that precedent:
`LinksNamedMoreThanOneParent`, set where the resolution is already computed, counted by `WorkItemService`
into `SyncOutcome.RecordsWhoseLinksNamedMoreThanOneParent`, and written to the row by both updaters.
`IWorkTrackingConnector` keeps every signature it had.

**What the number means, and where it means something narrower.** On a whole-query refresh it is how
many of the records the query holds could not be placed. On a cheap (delta) refresh only the records
whose stamp moved are read at all, so it is how many of *those* could not be placed — an untidy record
that sat still is silent until the next whole-query refresh reads it again. This is the same trade
`ReportLinksThatMeantNothingHere` already makes, and the same one the delta epic accepted for its own
counts; it is recorded on `SyncOutcome` in prose rather than papered over.

**A Portfolio's parent sweep warns but does not count.** `CreateFeaturesFromIssues` runs for both halves
of a Portfolio refresh — the Features the Portfolio owns, and the parent Features those hang under — and
the warning fires from inside it either way. The parent half, however, deliberately stays out of
`SyncOutcome`, so that its download never disturbs the counts the summary line reports for the Feature
half. The consequence is that a Portfolio refresh can log a warning naming three records while refresh
history reports zero for that run. This predates this slice and was not introduced by it; changing it
means changing what every count on `SyncOutcome` measures, which is a wider decision than a count of
unplaceable records justifies. Recorded here rather than fixed, so the next reader of a zero that
contradicts a warning knows which of the two to believe: the warning.

## Dependencies

Slice 02 — the resolver has to exist before a second match means anything.

## Effort

~3h. The counting is trivial; the warning's wording and its route to an existing surface is the work.

## Pre-slice SPIKE

No. The hypothesis is measured from slice 02's own run against the replicated instance, so the number is
already in hand before this slice starts. Read it out of slice 02's brief and record it above.

## Dogfood moment

Same day: point a Portfolio at the *same* link type its Team uses — the most likely real
misconfiguration (D7) — and watch every Feature with children refuse and say why, instead of quietly
taking one child as its parent.
