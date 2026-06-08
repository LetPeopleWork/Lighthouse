# DISTILL upstream issues (back-propagation) — multiple-cycle-times (Epic 5251)

Date: 2026-06-08. Gaps/contradictions DISTILL found in prior-wave artifacts.

## 1. Stale "low-sample state" wording in slice docs (D9) — NON-BLOCKING, doc fix

**Where:** `slices/slice-01-walking-skeleton-named-series-on-scatterplot.md` (lines 27, 40) and
`slices/slice-05-portfolio-scope.md` (line 24).

**Issue:** Both slice docs describe a "sparse/empty series low-sample state" / "explicit low-sample
state, not percentile lines on one or two dots." This **contradicts the locked D9**, which was refined
on 2026-06-08 (see `feature-delta.md` D9 + "Refinements folded in" note, and the journey YAML D9 +
changelog): a named series behaves **exactly like the default cycle time** for few items — it plots
whatever closed items crossed both boundaries and shows the item count + 50/70/85/95 percentile lines,
with **NO threshold and NO special low-sample UI state**.

**Resolution:** The authoritative SSOT (feature-delta Locked decisions + journey YAML) is unambiguous and
**user-confirmed**. Slices are planning docs, not `wave-decisions.md`, so this is **not** a
DISCUSS↔DESIGN reconciliation contradiction — it is stale slice prose predating the D9 refinement.
Acceptance scenarios are written to the locked D9 (no special state): see
`NamedCycleTimeReadApiIntegrationTest.FewQualifyingItems_StillPlotsThemWithCountAndPercentiles_NoSpecialLowSampleState`
and the Portfolio twin. **Recommend** the slice-01/05 docs be corrected at DELIVER time (one-line edit)
to drop the "low-sample state" language; left as-is for now to avoid mid-wave churn.

## 2. D8 "NEW read endpoint" realised as additive `definitionId` — ALREADY RECONCILED

**Where:** `feature-delta.md` D8 says "a NEW read endpoint serves per-definition scatter/percentile data";
DESIGN ADR-062 / DES-2 extends the EXISTING `cycleTimeData`/`cycleTimePercentiles` with an optional
`definitionId` instead.

**Resolution:** Not an open contradiction — explicitly documented and **user-confirmed (2026-06-08)** as a
refinement of D8's *mechanism* (per-definition read still served; no new write contract; additionally
removes the client version-gate touch-point). Recorded in `feature-delta.md` DESIGN Open Questions and
`design/wave-decisions.md` "Upstream DISCUSS changes". Scenarios target the additive-param contract.

No blocking items. Reconciliation gate: **PASSED — 0 contradictions.**
