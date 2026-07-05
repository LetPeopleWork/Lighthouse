# Epic 5074 — DESIGN-wave Upstream Changes (back-propagation to DISCUSS)

Feature-id: `epic-5074-blocked-items` | Wave: DESIGN | Date: 2026-06-12 | Decider: Morgan (PROPOSE)

DESIGN surfaced three points that require a DISCUSS/acceptance-design confirmation. None block DESIGN; all are recorded for the product-owner / acceptance-designer (DISTILL) to confirm. The architecture supports the proposed resolution for each.

---

## UC-1 (slice-04 AC2) — RESOLVED at DESIGN: blocked-duration is the driver, days-in-state is context

**DISCUSS AC2 (US-04 / slice-04)**: "An item that is both blocked-stale and state-stale renders stale ONCE, listing both reasons ('blocked 12 days' and '13 days in Review')."

**DESIGN finding (ADR-070)**: ADR-026's blocked-excludes-stale rule is PRESERVED for the TIME-IN-STATE trigger — a blocked item is NOT time-in-state-stale (its in-state clock is paused by the block). Therefore an item CANNOT simultaneously hold TWO staleness DRIVERS while blocked.

**RESOLUTION (architecturally DECIDED in ADR-070, reading (a) — not deferred)**: `StalenessResult.reasons` carries a `blocked-duration` DRIVER plus a `context-time-in-state` CONTEXT entry. The item is stale ONCE (driven by blocked-duration); its days-in-state is shown as context, not as a second trigger. `isStale` is computed only from driver kinds. AC2's "lists both reasons" is satisfied by the driver+context split. **Action**: acceptance-designer reflects the driver/context distinction in the AC2 Gherkin at DISTILL (wording only; the architecture is fixed). No product decision is open.

---

## UC-2 (slice-03 AC3) — historical per-TYPE blocked-count filtering not served by the total-count snapshot

**DISCUSS AC3 (US-03 / slice-03)**: "The chart respects the active team/portfolio/type/date-range filter (count reflects the filtered scope)" + the "filter to work-item type Bug" scenario.

**DESIGN finding (ADR-069)**: the forward-only `BlockedCountSnapshot` stores the TOTAL in-scope blocked count per owner per day. Team/portfolio scope and date-range are served (they scope the owner + window). A HISTORICAL per-work-item-TYPE breakdown over time would require recording per-type counts (a per-type snapshot column / row), which the single total-count column cannot reconstruct after the fact (forward-only — no retroactive per-type split).

**Proposed resolution**: slice 03 records and charts the TOTAL blocked-count trend (team/portfolio/date-range filterable). Per-type historical filtering is an ADDITIVE follow-up (a per-type snapshot column, no contract break). **Action**: confirm with the delivery-lead persona that the total-count trend satisfies the slice-03 outcome (judging clear-rate), with per-type deferred. No architecture is blocked; the snapshot schema is forward-compatible with a later per-type column.

---

## UC-3 (slice-05 / R3) — PRE-SLICE SPIKE confirmed REQUIRED

**DISCUSS R3 / slice-05 weight note**: "SPIKE candidate IF the additional-field list is deeply user-CRUD-coupled."

**DESIGN finding (ADR-071, evidence from brownfield)**: the value/fetch/rule paths are fully GENERIC (favourable), but the predefined-field EXCLUSION threads through FOUR surfaces (CRUD reconcile, license slot gate, DTO projection split, connector auto-registration) and there is NO existing precedent for a system-registered field. This crosses the R3 SPIKE threshold.

**Resolution (VERDICT: SPIKE)**: a timeboxed (~half-day) pre-slice-05 SPIKE is required before slice 05 is sized/committed, answering: reconcile-merge (no silent delete on user PUT), license slot-count split (user vs total), `WriteBackMappingDefinition` compatibility + `Reference` immutability, single-hook idempotent auto-registration, and the FE DTO list/picker split. **Action**: schedule the SPIKE at the start of slice 05; does NOT block slices 01–04 (slice 05 is MoSCoW Could, last). Re-size/re-slice slice 05 if the SPIKE finds coupling beyond the four touch-points.

---

## Summary

| Ref | Slice / AC | Type | Blocks DESIGN? | Action owner |
|---|---|---|---|---|
| UC-1 | S04 AC2 | RESOLVED at DESIGN (driver+context); Gherkin wording only | No | acceptance-designer (DISTILL — wording) |
| UC-2 | S03 AC3 | scope clarification (per-type deferred) | No | product-owner / delivery-lead (DISTILL) |
| UC-3 | S05 / R3 | SPIKE gate confirmed | No (slices 01–04 proceed) | software-crafter (pre-slice-05 SPIKE) |
