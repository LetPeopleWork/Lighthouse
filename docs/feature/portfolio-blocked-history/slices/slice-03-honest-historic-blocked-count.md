# Slice 03 — Honest Blocked count on a historic portfolio range

**Story**: US-03 | **ADO**: #5524 | **Effort**: ~0.7 crafter-day

## Goal

Make `GetInProgressFeatures` answer a past date range from captured blocked history instead of replaying today's rules against today's feature — mirroring the guard `TeamMetricsController` already uses.

## Why third

The story's headline ask, and the slice that retires the public docs caveat. It runs after slice 02 because it consumes that capture, and after slice 02's hypothesis has been settled, so it is implementation of a proven pattern rather than a bet.

## IN scope

- `isHistoricRange` guard in `PortfolioMetricsController.GetInProgressFeatures`, the same shape as `TeamMetricsController.cs:130` (`asOfDate.Date < DateTime.UtcNow.Date`).
- Historic answers read from feature blocked spells; live rule evaluation retained as fallback for features with no capture history at all.
- `blockedSince` on a historic read reports the spell's `EnteredAt`, not the live `CurrentStateEnteredAt`.
- **Remove the caveat paragraph at `docs/metrics/widgets.md:106`** and make the Blocked Overview section read identically for Teams and Portfolios (D6).

## OUT of scope

- Drill-through membership — slice 04.
- Retrospective backfill for real customers. Capture is forward-only exactly as it was for teams; a pre-capture range falls back to the live rule.
- Changing the guard shape. If the team guard is wrong, that is a separate finding against #5508, not a divergence to introduce here.

## Learning hypothesis

**Hypothesis**: the team-side historic-read pattern transfers to portfolios unchanged.

- **Confirmed if** the same blocked-spell scenario asserted through both controllers over the same past range produces the same answer.
- **Disproved if** portfolio semantics force a different guard or a different fallback rule — which would most likely mean D8 (multi-portfolio membership) was resolved in a way that makes "the feature's history" ambiguous per portfolio. That is a DESIGN escalation, not a local fix.

## Acceptance criteria

See US-03 AC1–AC5 in `../feature-delta.md`. Summary: past-blocked-now-clear reads blocked (AC1), blocked-only-today reads clear on earlier ranges (AC2), guard matches the team shape (AC3), no-history falls back to live while has-history-no-spell reads not-blocked (AC4), docs caveat gone (AC5).

## Dependencies

- Slice 02 (there is no history to read without it).

## Reference class

Story #5508's team-side equivalent (`TeamMetricsController.cs:124-156`) was a contained change to one action method plus two repository queries. This is the same change against a different controller, plus the docs edit.

## Risks

- **The inverse error is the easy one to miss.** A feature blocked *today* must read not-blocked on a range that closed before the spell began. Test both directions — AC2 exists because the ADO description called this out explicitly.
- **The fallback is a correctness cliff.** "No history at all → live rule" is right; "no spell covering this date → live rule" is wrong, and would silently reintroduce the bug for every feature that has ever been blocked. AC4 draws that line; the test must pin it.

## Notes for the crafter

- `TeamMetricsController.cs:130-156` is the reference implementation, comment included — the `UPSTREAM-7` comment explains the reasoning and is worth mirroring in spirit.
- Two repository queries back the team version: an at-date spell lookup and a has-any-history lookup, the latter indexed rather than scanned per item (see commit `1d4dcb5a`). Do not reintroduce the per-item scan on the portfolio path.
- The docs edit is part of **this** slice, not deferred to `/release`. If `/release`'s `update-docs` pass later finds this caveat still present, the per-feature discipline was skipped.
