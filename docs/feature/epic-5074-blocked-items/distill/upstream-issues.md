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

## UC-3 (slice-05) — NOW IN SCOPE (SPIKE WAIVED 2026-07-11)

Slice-05 (predefined/system Jira flagged field) was originally gated behind a timeboxed pre-slice-05
SPIKE. Per **ADR-071 Amendment A** (user decision 2026-07-11) the SPIKE is **WAIVED**: the five SPIKE
questions were answered at design time and are carried into DELIVER as enumerated tests. Amendment B
promotes auto-registration to the `IWorkTrackingConnector.GetPredefinedAdditionalFields` **port method**.
Slice-05 acceptance tests are authored in this DISTILL delta — see the DISTILL / [REF] slice-05 section
of `feature-delta.md` and the slice-05 rows of `red-classification.md`. The Wave-Decision Reconciliation
gate re-ran across DISCUSS / DESIGN / DEVOPS for slice-05 with **0 contradictions**.

## UC-5 (slice-05, DELIVER concern) — auto-registration must be observable in the WebApplicationFactory AT host

**Not a spec contradiction — a DELIVER wiring note surfaced while authoring the slice-05 ATs.** Six of
the slice-05 backend scenarios assert on a *served, auto-registered* predefined "Flagged" field
(reconcile merge-back, slot split, port seam, idempotency, inbound-only/immutability). Auto-registration
fires on a Jira connection's sync/setup and resolves `Reference` from the connection's
`FieldNames[...][FlaggedName]` — a path that today needs a live Jira. In the black-box
`WebApplicationFactory` host there is no live Jira, so these ATs are **RED now** (no predefined field is
surfaced) and are only GREEN-able once DELIVER wires the registration to fire deterministically in the
test host.

**Recommended DELIVER seam** (mirrors how `ILicenseService` is already faked in
`BlockedItemsAcceptanceTest`): register a test double / real `IWorkTrackingConnector` (or the
registration service that consumes `GetPredefinedAdditionalFields`) via DI in the WAF so the predefined
"Flagged" field is auto-registered for a Jira connection without a live Jira call, and invoke the
registration at connection setup (or a sync-setup hook) that the connection GET reflects. The ADR-071
"Architectural Enforcement" unit tests (on `GetPredefinedAdditionalFields`, `SupportsAdditionalFields`
counting `where !IsPredefined`, and `UpdateAdditionalFieldDefinitions` merge-back) are authored in
DELIVER once the `IsPredefined` member + port method exist — they sharpen these black-box ATs at the
unit layer. **Action owner**: software-crafter (DELIVER). No architecture is blocked; the ATs pin the
observable contract.

### DELIVER gates (from the DISTILL Final Wave Review, 2026-07-11)

The DISTILL review (Sentinel `approved`; Architect `conditionally_approved`) surfaced no design or test defect — the Architect's conditions are DELIVER accountability gates, recorded here so they cannot be skipped:

1. **Port method declared first** — `IWorkTrackingConnector.GetPredefinedAdditionalFields(WorkTrackingSystemConnection)` must be on the interface (signature exactly per ADR-071 Amendment B) before any slice-05 production code lands.
2. **Five enforcement tests pass before merge** — reconcile merge-back, slot-count `where !IsPredefined`, write-back-target exclusion + `Reference` immutability, auto-registration idempotency, FE DTO `isPredefined` split (ADR-071 Architectural-Enforcement table).
3. **Deferral is the escape valve** — if any enforcement test reveals coupling beyond the four known surfaces, slice 05 is re-sliced or deferred (MoSCoW Could), not carried forward with debt.
4. **Code-review check** — verify the merge-site call to `GetPredefinedAdditionalFields` (sync/connection-setup hook) is not over-coupled; the port is on the driven `IWorkTrackingConnector` boundary by design.

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
