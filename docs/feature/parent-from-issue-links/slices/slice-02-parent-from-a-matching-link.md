# Slice 02 — A parent read from a matching link, whichever end it is on

**Feature**: parent-from-issue-links · **ADO**: pending · **Story**: US-02 · **Estimate**: ~5h
**Reference class**: `IssueExtensions.ExtractDependencyReferences` (`:61-80`) — the same `issuelinks`
payload, walked the same way, matching a type name instead of the `BlockedByLinkName` const. The parser
this slice needs has a working sibling to copy.

## Goal

On a Team or Portfolio whose Parent Override Field names a link type, every item with exactly one link of
that type gets its counterpart issue key as its parent.

## IN scope

- A resolver over `issuelinks`: for a given link type name, collect the counterpart issue key of every
  matching link — `outwardIssue` where this item holds the inward end, `inwardIssue` where it holds the
  outward end. Match on `name`, `inward` or `outward`, case-insensitively (AC-2.4).
- One call site: `CreateWorkItemFromJiraIssue` (`JiraWorkTrackingConnector.cs:1570-1575`). When the
  override's Additional Field resolved to a link type rather than a field, the parent comes from the
  resolver instead of from `GetAdditionalFieldValue`.
- **Both grains for free, and asserted anyway.** That one method is called from `CreateWorkItemsFromIssues`
  (`:285`) for a Team and `CreateFeaturesFromIssues` (`:1174`) for a Portfolio. The tests prove both;
  the implementation serves both once.
- The resolved key is written into the Additional Field's value on the work item (D9), where `""` sits
  today.
- Exactly-one and zero only. Two or more is slice 03's; until then such an item takes no parent, which is
  the end state anyway — slice 03 adds the warning, not the behaviour.
- Request-count assertion against the pre-change baseline (AC-2.7).

## OUT of scope

- The ambiguity warning (slice 03). The refusal behaviour is here; the explanation is not.
- Any other connector (slice 04, D6).
- The docs. They land at feature finalization with the screenshots.

## Learning hypothesis

**Disproves, if it fails**: D3 — that Jira serves each link from both ends, so one pass over the items a
query already returns covers both directions, and no child→parent index is needed.

That claim rests on `IssueExtensions.cs:53-57`, which states it as the reason dependencies deliberately
read one end only. It is a comment about the behaviour of someone else's API, written for a neighbouring
purpose, and this slice is the first thing to depend on it for the opposite reason.

**The test that settles it**: on a real instance, create a link from a Portfolio-level item down to two
team-level items, then fetch *only the team-level items* with a query that does not include the
Portfolio-level one. If each returns the link with the Portfolio-level item as its `inwardIssue`, D3
holds and the feature is a one-pass read. If they come back with no `issuelinks` at all, the reverse
direction genuinely needs the index, this slice is roughly twice the size, and the estimate above is wrong.

**Confirms, if it succeeds**: direction never has to be configured, asked about, or explained — which is
what makes D1's single unchanged setting possible.

## Acceptance criteria

AC-2.1 through AC-2.7 in `feature-delta.md`. AC-2.6 is the production-data criterion and is not optional.

## Production data

A Jira Data Center instance configured the way Steve's customer configured theirs. Record here, as
numbers: how many Features carried `IsUsingDefaultFeatureSize` before, how many after, and how many of
the remainder genuinely have no children.

## Dependencies

Slice 01 — a link type has to be nameable before it can be read. P3 (a replicable instance) for AC-2.6.

## Effort

~5h. One resolver, one branch at one call site, tests across both grains and both link directions. The
estimate assumes D3 holds; if the spike below says otherwise, re-estimate before starting.

## Pre-slice SPIKE

**Yes, 30 minutes, and it is the hypothesis test above.** Run the two-child fetch against a real instance
and look at whether `issuelinks` comes back on the children. Everything in this slice is sized on the
answer.

## Dogfood moment

Same day: point the replicated Team at the registered link type, refresh, open the Portfolio. The
no-children warnings going away is the demo.

## Changed Assumptions

**Original (this brief, Production data, 2026-09-18):** "A Jira Data Center instance configured the way
Steve's customer configured theirs. Record here, as numbers: how many Features carried
`IsUsingDefaultFeatureSize` before, how many after."

**New (user, 2026-09-18, DESIGN wave):** no Data Center instance is available. AC-2.6 is met with
fixtures built from `TrackerWireFormats.cs`, plus an explicit statement that it was not run on production
data.

**Consequence, stated rather than absorbed:** AC-2.6 exists because the carpaccio taste test rejects a
slice proved only by synthetic data — "it proves plumbing, not value". Meeting it with fixtures is
exactly the thing that test refuses, so this is a knowing exception and not a pass.

What still closes honestly: the hypothesis itself. Whether Jira serves a link from both ends (D3) is
testable on Cloud, and if it holds there it holds on Data Center, because it is a property of how Jira
models a link rather than of a deployment. So the sizing risk this slice was built to retire is retired.

What does not close: that the reported customer's instance is fixed. The before/after numbers on a real
Portfolio are not obtainable, so `OUT-PFIL-hierarchy-recovered` has no reading and stays open.

**Owed:** one verification pass against a Data Center instance — Steve's, or a stood-up equivalent —
before this is reported to the customer as done.
