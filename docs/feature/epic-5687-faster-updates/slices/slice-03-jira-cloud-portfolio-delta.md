# Slice 03 — A Jira Cloud portfolio refresh fetches only the Features that moved

**Feature**: epic-5687-faster-updates · **ADO**: Epic #5687 → Story #5726 · **Story**: US-03 · **Estimate**: ~5h
**Reference class**: slice 02. This is that contract pointed at `RefreshFeatures` and
`RefreshParentFeatures` instead of `RefreshWorkItems`.

## Goal

A portfolio update downloads full Feature payloads — and parent Feature payloads — only for Features
whose `updated` timestamp moved, so the saving slice 02 won on the team half is not handed straight back
on the portfolio half.

## IN scope

- `LastChangedRemote` on the Feature (additive, expand-only, same migration pattern as slice 02).
- The slice-02 sweep applied to `GetFeaturesForProject`.
- The parent-Feature path (`GetParentFeaturesDetails`) — a keyed `key = "X" OR key = "Y"` query today,
  so its sweep is the same query with `fields=updated`.
- Feature state transitions and blocked transitions left untouched for unchanged Features.
- A Feature shared across portfolios is fetched once per cycle and applied to every portfolio that
  claims it.
- **Derived work still recomputes every cycle regardless of mode** (D9): `RefreshRemainingWork`,
  `ExtrapolateNotBrokenDownFeatures`, the percentile default size, and the forecast trigger. These depend
  on wall-clock and on *other* teams' throughput, so skipping them because this portfolio's Features did
  not move would be wrong.
- Portfolio summary line reports `mode` and both counts.

## OUT of scope

- Jira Data Center (slice 04).
- The fetch fingerprint (slice 05).
- Any change to how remaining work or extrapolation is *calculated* — only to how often the inputs are
  refetched, which is: exactly as often as they change.
- Any UI.

## Learning hypothesis

**Disproves "the delta contract generalises from work items to Features without a second design."** Two
ways it fails:

1. **The Feature is not a leaf.** A Feature's stored state depends on its children's work items
   (`RefreshRemainingWork` walks `workItemRepository` by `ParentReferenceId`). If "the Feature did not
   change remotely" gets confused with "the Feature's rollup did not change", the portfolio numbers go
   stale while every Feature row looks fresh — a wrong number with a green sync. D9 exists to prevent
   exactly this, and this slice is where it is proven rather than asserted.
2. **The parent path has no stable query.** `GetParentFeaturesDetails` builds a key list from whatever
   the children referenced this cycle. If that list itself is derived from the fetched set rather than
   the stored set, delta shrinks it and parents silently drop out.

Confirms, if it succeeds: slices 04/06/07/08 are transport work, and no further contract design is owed.

## Acceptance criteria

AC-3.1 … AC-3.6 from `feature-delta.md` (US-03). AC-3.5 is the one that catches failure mode 1.

## Dependencies

- Slice 02 (the contract, and the summary line's `mode` field carrying real values).
- The same real Jira Cloud project, with a portfolio configured over it.

## Effort

~5h. Feature migration ~0.5h, `RefreshFeatures` sweep ~1.5h, parent path ~1h, D9 guard tests ~1h,
remaining tests ~1h.

## Production data / dogfood moment

Run a portfolio over the real Jira Cloud project through one full cycle and one delta cycle, then confirm
the portfolio's forecast dates are identical across the two. Same day. A portfolio whose Features are all
synthetic proves the plumbing and none of the rollup risk.

## Pre-slice SPIKE

Not needed.

## Verdict

**Confirmed, mechanism and cost.** Recorded 2026-08-12, retroactively — the block was left at its
placeholder when the slice shipped on 2026-08-11 as `593d11131..2c443ea59`.

The hypothesis was *"the portfolio half is the team half with a different noun"*. It is not, and the
difference is the whole value of the slice. A team refresh removes what the query stopped returning; a
portfolio refresh **rebuilds itself from what was fetched** — `Portfolio.UpdateFeatures` is a Clear +
AddRange. Under delta, every unchanged Feature therefore loses its portfolio claim, and
`OrphanedFeatureCleanupService.CleanupAsync` — which the updater runs in its own `finally` — then deletes
anything left with `!IsParentFeature && !Portfolios.Any()`.

That was demonstrated, not theorised: at step 02-02 the crafter fed the fetched list straight back into
`portfolio.UpdateFeatures` and the scenario reported `FEAT-1, FEAT-3` MISSING, deleted, with
`FeatureUnblocked` raised on the way out. Sequencing the survivor rule first — while every cycle was still
full — is what kept that out of a commit.

Survivors are resolved by reference id through `featureRepository`, never out of `portfolio.Features`. A
probe at step 03-02 proved no test in the fixture catches a regression there, because every portfolio in
it is pre-refreshed over the same Features. That lookup is load-bearing and must not be "simplified".

**Live dogfood on a real Jira Cloud project**, 2026-08-11:
`mode=Full scanned=4 fetched=4 3555ms` → `mode=Delta scanned=4 fetched=0 1092ms` → after one state change
`mode=Delta scanned=4 fetched=1 1944ms`. Mechanism confirmed end to end.

**Mutation: 78.69 % on slice-03 changed lines — the 80 % gate was missed by one mutant**, recorded rather
than padded. All 13 survivors were verified equivalent by application; on non-equivalent mutants it is
48/48. The pass killed five, including a `SweepDepartedFeatureSpells` deletion that slice 01 had recorded
and nobody closed (a Feature blocked when it left the query accrued blocked time forever) and the whole
parent-inversion rule, which had no test at all. It also found `AddProjectToFeature` to be dead code — the
many-to-many writes the join row from `Portfolio.UpdateFeatures`, so the per-Feature loop wrote what the
next line already wrote — and it was removed.

CI green including `sonar-gates`. ADO #5726 **Closed**. Still Jira Cloud only; Data Center waited on OQ-1,
which slice 04 now has an answer for.
