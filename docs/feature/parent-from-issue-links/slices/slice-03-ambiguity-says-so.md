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

## Acceptance criteria

AC-3.1 through AC-3.5 in `feature-delta.md`.

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
