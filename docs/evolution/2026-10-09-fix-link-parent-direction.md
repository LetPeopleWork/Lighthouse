# Bug #6203 — a typed link label sets which way parents are read

Delivered 2026-10-09, pushed to `main` at `21555fd65` (CI green). Reported by Steve Pereira, following up on
the parent-from-issue-links Epic (#6028).

## Summary

With Parent Override Field naming a Jira issue link type, every item that was both a child and a parent got
no parent. The customer draws a whole hierarchy with one link type: a child reads *is caused by* its parent,
a parent reads *results in* its children. Only leaves were placed, so child counts per Feature stayed at
about 1.

## Root cause

Two layers, both needed to explain it:

1. `IssueExtensions.ResolveParentFromLinks` took the far end of every link of the configured type, in both
   directions. An item in the middle has one link up and N links down, so it had N+1 candidates, and
   `ParentSourceSelector` refused it as ambiguous, which is the right call for genuinely ambiguous items.
2. `JiraWorkTrackingConnector` resolved the typed reference against the instance's link types and kept only
   the type's name (`ResolvedReference.ALinkType(answeringTypes[0].Name)`). The direction the administrator
   expressed by typing one end's phrase was lost before the resolver ran, which is why typing the other
   label changed nothing.

The original design assumed an item's links of that type only ever point at its parent. That holds for two
levels and fails for three or more.

## What changed

- `LinkDirection` (`Both`, `Inward`, `Outward`) travels on `ResolvedReference` beside the type name.
- `JiraIssueLinkType.DirectionReadFrom` decides it from the typed text: the type's name wins and reads both
  ways (Jira's stock *Blocks* type is named the word its outward end reads); only the inward phrase → inward
  ends; only the outward phrase → outward ends; a phrase both ends share (*relates to*) → both ways.
- `ResolveParentFromLinks` keeps only the ends that direction names; `Both` is exactly the old behaviour.
- Team and Portfolio edit docs now say the typed phrase sets the direction, and when to type which.

## Decisions

| Decision | Why |
| --- | --- |
| The typed phrase sets the direction; no new setting | The customer's configuration starts working unchanged. |
| A type-name match wins over a label match | Keeps every configuration that typed a name reading both ways, as before. |
| Live Jira regression fixture | The maintainer asked for a real-instance test. Demo Jira now has link type *Causes* (inward *is caused by*, outward *causes*) and `LGHTHSDMO-8430` ← `8431` ← `8432`, `8433`, label `ParentLinkDirection`. |

## Behaviour change to call out in release notes

Someone who typed a **one-way** phrase for a single-level hierarchy, and whose children hold the *other*
end of the link, used to be placed (both ends were read) and now is not. The docs now steer to the phrase a
child reads towards its parent, or the type's name when in doubt.

## Quality gates

- Tests: unit (`IssueExtensionsParentLinkTest`), connector acceptance (`ParentFromIssueLinks` Slice 02, five
  new scenarios), live (`JiraParentLinkDirectionDogfoodTest`). Filtered backend suite 9422 passed, 0 failed.
- Adversarial review (Opus): production correct; two untested branches of `DirectionReadFrom` (symmetric
  phrase, outward phrase) closed with acceptance scenarios proven red under their mutants.
- Mutation: every mutant on a changed line killed — Stryker 4/4 on `IssueExtensions.cs`, 8/8 hand probes on
  `JiraWorkTrackingConnector.cs`. Whole-file `IssueExtensions.cs` 73.44 %, held down by pre-existing shape
  guards. Details: `docs/feature/fix-link-parent-direction/mutation/results.md`.

## Commits

`92e28a80b` fix · `8e7841504` refactor · `dde418fc4` review tests · `00be48a76` mutation results ·
finalize (this doc + docs pages).

## Follow-ups (not done)

- The per-link matcher still counts a link of a *different* type whose phrase equals the resolved type's
  name. Pre-existing; the root fix is to match links by type name only.
- Six pre-existing `LabelOf`/`KeyOf` shape-guard survivors; one malformed-link test would kill them.
- Story #6251: Feature Size and Feature Size Percentiles on the Team — Steve could not find the chart because
  it exists only on Portfolios.

## Lessons

- A test fixture drawn at two levels cannot catch a defect that only appears at three. The original live
  fixture had exactly that shape.
- Jira's `POST /issueLink` names the cause as `inwardIssue`; on the child the counterpart then appears as
  `inwardIssue`. Read a link back before trusting its direction.
- Stryker.NET generates no mutants for switch-expression arms; probe those by hand.
