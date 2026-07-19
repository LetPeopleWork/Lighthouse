# Slice 05 — Demo portfolio blocked history worth clicking

**Story**: US-05 | **ADO**: #5524 | **Effort**: ~0.5 crafter-day

## Goal

Re-add backdated blocked history for demo portfolios — this time into the feature keyspace — so a freshly-loaded demo shows a portfolio blocked trend whose bars drill into real features and whose features show real durations.

## Why last

It re-adds, correctly, what slice 01 withdrew. Running it last means **no earlier slice can hide behind synthetic data**: slices 01–04 must each prove themselves against real refresh flows before demo convenience returns. This is deliberate, not incidental.

## IN scope

- Backdated feature blocked spells in the feature keyspace (D3), synthesized on `PortfolioFeaturesRefreshed`.
- Demo gating via `SynthesizeStateJourneyForDemo` and idempotency, matching the existing handler's contract exactly.
- `StartedDate` cap preserved — never claim a feature was blocked before it started.
- Verify against the loaded demo dataset that the portfolio chart, the drill-through dialog and the duration badge all populate.

## OUT of scope

- Any change to real-customer data paths. The demo gate is the whole safety argument; if a code path can run for a non-demo connection, the slice is wrong.
- Changing the 14-day `HistoryWindowDays` or the spread algorithm. Reuse `SpreadEnteredDates` as-is — matching team demo behaviour matters more than tuning it.
- Re-adding team-side transition writes. Slice 01's invariant (US-01 AC3) must still hold after this slice.

## Learning hypothesis

**Hypothesis**: demo backfill can populate the feature keyspace with the same shape it used for teams, and the resulting portfolio surfaces are indistinguishable in quality from the team ones.

- **Confirmed if** a fresh demo load produces a climbing portfolio trend, non-empty drill-through on past bars, and a spread of `blocked Nd` values.
- **Disproved if** the feature keyspace needs a materially different synthesis shape — which would suggest D3 or D8 was resolved in a way that makes feature spells structurally unlike work-item spells, worth reporting back.

## Acceptance criteria

See US-05 AC1–AC4 in `../feature-delta.md`. Summary: spells land in the feature keyspace and slice 01's invariant still holds (AC1), demo-gated and idempotent (AC2), fresh demo shows non-empty drill-through and non-zero durations (AC3), `StartedDate` cap preserved (AC4).

## Dependencies

- Slice 01 (established the invariant this must not break).
- Slice 02 (the keyspace to write into).
- Slices 03 and 04 benefit from this for manual verification, but must not *depend* on it — if either needed demo data to demonstrate value, it was sliced wrong.

## Reference class

The handler already exists and already has a portfolio branch; this retargets its writes. Epic-5074 slice-07 built the original in well under a day.

## Risks

- **Idempotency guard uses snapshots, not transitions.** The existing check is "a backdated snapshot exists ⇒ already backfilled". After slice 01, demo portfolios will already have backdated *snapshots* but no transitions — so the existing guard will short-circuit and skip transition synthesis entirely. **This slice must reconcile that**, or it will silently do nothing on exactly the instances it targets. This is the single likeliest way this slice ships broken.
- The dispatcher swallows handler errors: verify by inspecting resulting rows, not by absence of exceptions.
- Use `GetAllByPredicate(...).Any()` for the guard scan; `GetByPredicate` and `Exists` use `SingleOrDefault` and throw on multiple matches.

## Notes for the crafter

- Existing guard: `DemoBlockedHistoryBackfillHandler.cs:118-127`.
- Screenshot regeneration for any changed portfolio surface: `rm` the old PNG **first** — the `@screenshot` comparison keeps the old image when the diff is under 0.5%, so a real change can silently fail to land.
