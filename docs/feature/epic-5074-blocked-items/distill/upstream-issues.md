# Epic 5074 — DISTILL Upstream Issues / Back-Propagation

Feature-id: `epic-5074-blocked-items` | Wave: DISTILL | Date: 2026-07-03

DISTILL authored the acceptance suite for slices 01–04. The Wave-Decision Reconciliation HARD GATE
passed with **0 contradictions** between the DISCUSS and DESIGN sections of `feature-delta.md`. The
following are not contradictions — they are DESIGN-flagged confirmations (`design/upstream-changes.md`)
whose DISTILL disposition is recorded here, plus minor notes.

## UC-1 (slice-04 AC2) — RESOLVED at DESIGN; DISTILL applied the wording

DESIGN (ADR-070) resolved that a blocked-and-also-state-aged item is stale **once**, with a
blocked-duration **driver** reason plus a time-in-state **context** reason (a blocked item's in-state
clock is paused, so it cannot hold two staleness *drivers*). Since staleness is FE-derived
(`deriveStaleness` selector), the AC2 rendering is a **Vitest** scenario authored in DELIVER, not a
backend HTTP AT. The backend contract this DISTILL drives is the `blockedStalenessThresholdDays`
setting + the `blockedSince` capture that feed the selector. **No open product decision.**

## UC-2 (slice-03 AC3) — CONFIRMED: per-TYPE historical filtering deferred

DESIGN (ADR-069) established that the forward-only `BlockedCountSnapshot` stores the **total** in-scope
blocked count per owner per day. Team/portfolio scope and date-range are served; a **per-work-item-type
historical breakdown is not reconstructable** from a total-count column (forward-only, no retroactive
per-type split).

**DISTILL disposition**: the slice-03 scenario `The_blocked_trend_can_be_filtered_to_a_single_work_item_type`
is authored but tagged **`@deferred`** and `[Ignore]`d with a DEFERRED reason (not a plain pending
marker). It is **out of scope for this DISTILL delivery** and must NOT be enabled in slice-03 DELIVER.
It becomes enable-able only after the additive per-type snapshot column ships as a follow-up (no
contract break — the snapshot schema is forward-compatible).

**Action owner**: product-owner / delivery-lead to confirm the total-count trend satisfies the slice-03
outcome (judging clear-rate) with per-type deferred. The architecture is not blocked.

## UC-3 (slice-05) — OUT OF DISTILL SCOPE (pre-slice-05 SPIKE gate)

Slice-05 (predefined/system Jira flagged field) is gated behind a timeboxed pre-slice-05 SPIKE
(ADR-071 §5). It was **explicitly excluded** from this DISTILL run and no slice-05 acceptance tests
were authored. Slices 01–04 do not depend on it.

## Notes (non-blocking)

- **Outcomes registry**: `docs/product/outcomes/registry.yaml` does not exist in this project → OUT-N
  registration **skipped** (per the register-outcomes procedure: skip when the registry is absent). The
  new typed contract surfaces introduced by slices 01–04 (`blockedRuleSetJson`, `IBlockedItemService`,
  `WorkItemBlockedTransition` / `WorkItemUnblocked`, `BlockedCountSnapshot` + `blockedCountHistory`
  endpoint, `blockedStalenessThresholdDays`) are documented in the DESIGN component decomposition; if
  the registry is later adopted, register them then.
- **Boundary semantics (OQ1)**: DESIGN resolved blocked-staleness uses `≥ blockedStalenessThresholdDays`
  (time-in-state keeps `>`). The slice-04 exact-at-threshold boundary is covered by FE `deriveStaleness`
  Vitest in DELIVER (the backend only stores the threshold); a backend boundary AT is not applicable
  because the backend does not compute stale.
- **Contract-shape tags**: the 2026-05-15 `@contract-shape:` Gherkin-tag mandate is a Python-pilot
  construct; this project's `atdd-infrastructure-policy.md` governs with NUnit black-box conventions.
  Each scenario's contract shape is instead expressed through its assertion (unbounded-preservation for
  the walking skeleton's read-your-writes, bounded-change for the settings round-trips). Noted for the
  reviewer; no `.feature`/Gherkin-tag surface exists in this repo to carry the machine tag.
