# Slice 04 — Drill into a past portfolio blocked bar

**Story**: US-04 | **ADO**: #5524 | **Effort**: ~0.6 crafter-day

## Goal

Replace the live-only stub in `PortfolioMetricsController.GetBlockedItemsAtDate` with membership reconstructed from feature blocked spells, so clicking a past bar on the portfolio Blocked-Over-Time chart lists the features blocked that day.

## Why fourth

Same capture as slice 03, second read shape. Lowest learning leverage in the feature — by the time it starts, slice 03 has proven the pattern transfers, so this is mechanical. Epic-5074 shipped the team-side equivalents as separate slices (06 and 08) for the same reason.

## IN scope

- Reconstruct at-date membership from feature blocked spell intervals — **read-only, never persisted** (upholds the `blockedMembershipAtDate` source_of_truth).
- Latest/today bar continues to reconstruct from live `IsBlocked`, matching `TeamMetricsController.cs:512`.
- Pre-capture dates return the reconstructable set plus the completeness note.
- **Call the ADR-099 reconciliation guard** at `PortfolioMetricsController.cs:510` from the portfolio path. It already exists and is already parameterised by owner — only the call site is missing.
- Delete the now-false comment at `PortfolioMetricsController.cs:498-500` claiming reconstruction is impossible for portfolios.

## OUT of scope

- Any persisted membership column on `BlockedCountSnapshot`. Explicitly forbidden by `blockedMembershipAtDate`'s source_of_truth — reconstruction is the design, not a compromise.
- Frontend work. `WorkItemsDialog` and the chart's click handler are shared and already wired for portfolios.
- Fixing snapshot/reconstruction divergence if the guard starts firing. The guard's job is to **surface** capture gaps, not to reconcile them silently. If it fires, that is a finding to report, not a bug to paper over.

## Learning hypothesis

**Hypothesis**: reconstructed portfolio membership reconciles with the independently-captured `BlockedCountSnapshot` for the same date.

- **Confirmed if** reconstruction count equals snapshot count across the test window and the ADR-099 guard stays quiet.
- **Disproved if** the guard fires — which means feature spell capture (slice 02) has a gap the aggregate count does not, since the two are recorded by different handlers on the same event. That is genuinely valuable: it is the only cross-check the design has on slice 02's correctness.

This is the reason the guard call is IN scope rather than optional.

## Acceptance criteria

See US-04 AC1–AC5 in `../feature-delta.md`. Summary: past-date membership reconstructed (AC1), today reconstructs live (AC2), pre-capture dates carry the completeness note (AC3), ADR-099 guard called and logging divergence (AC4), obsolete comment removed (AC5).

## Dependencies

- Slice 02 (capture), slice 03 (proves the read pattern transfers).

## Reference class

`TeamMetricsController.cs:501-530` — the team drill-through, roughly half a day in epic-5074 slice-08.

## Risks

- **Slice-08 held finding carries over**: on the team side, `SyncDay` is fixed in the past for demo data, so the live/today branch was never exercised end-to-end. The same blind spot will exist here unless the test explicitly drives a today-dated request. Cover it.
- Reconstruction and the snapshot are written by two different handlers off one event. If the dispatcher swallows one handler's error, the two diverge silently — which is precisely what the ADR-099 guard catches. Do not disable it to make a test pass.

## Notes for the crafter

- The guard method already reads `ownerType` and `ownerId` — it was written to serve both scopes and has simply never been called from the portfolio path.
- The interval predicate is `EnteredAt < startOfNextDate && (LeftAt == null || LeftAt >= startOfDate)` (`WorkItemBlockedTransitionRepository.cs:26`). Mirror it exactly for features; an off-by-one on the half-open boundary is the likeliest defect here.
