# ADR-070: Blocked-Duration Staleness — A Distinct Trigger OR'd Into the Single `deriveStaleness` Selector with Multi-Reason Output; AMENDS ADR-026's Blocked-Excludes-Stale Rule

**Status**: Accepted (2026-06-12 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-12
**Feature**: epic-5074-blocked-items (Slice 04 — blocked→stale linkage)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: **AMENDS ADR-026** (cross-surface staleness derivation + blocked-excludes-stale) — see the `## Changed Assumptions` section, which quotes the original rule and states the superseding decision. Consumes `blockedSince` (ADR-068) and the single `IsBlocked` (ADR-067). Follows ADR-064/056 (additive settings field, no gate). Resolves DISCUSS D-STALE, D8, and OQ1 (the `≥` boundary).

---

## Context

Slice 04: a long-blocked item must read **stale** with the reason "stale: blocked 12 days", distinct from "stale: 11 days in Review". The blocked-staleness trigger keys on blocked DURATION (from `blockedSince`, ADR-068), is configured by a new `blockedStalenessThresholdDays` (0 = disabled, twin of `StalenessThresholdDays`), and OR's with time-in-state staleness — both reasons listed when both fire, never double-counted (D8).

This **directly contradicts a shipped business rule**. ADR-026 §Decision established that staleness derives client-side through a single pure selector `deriveStaleness(item, thresholdDays, now): boolean`, and that **a blocked item must NOT be flagged stale** — encoded as `&& !item.isBlocked`. Verified code reality (`src/utils/staleness/deriveStaleness.ts` L8–24):

```typescript
export const deriveStaleness = (item, thresholdDays, now = new Date()): boolean => {
  if (thresholdDays === undefined || thresholdDays <= 0) return false;
  if (item.isBlocked) return false;            // ← ADR-026 blocked-excludes-stale
  if (!item.currentStateEnteredAt) return false;
  return daysInState(item.currentStateEnteredAt, now) > thresholdDays;
};
```

If slice 04 naively made a blocked item stale, it would invert the exact clause ADR-026 added. The reconciliation must (a) PRESERVE ADR-026's intent — TIME-IN-STATE staleness still excludes blocked items (a blocked item is not "stale in Review" because its clock is paused by the block) — while (b) ADDING a separate blocked-DURATION staleness that fires precisely BECAUSE an item is blocked too long. The two are not in conflict once separated: blocked excludes TIME-IN-STATE stale; blocked-duration is its OWN stale source.

---

## Changed Assumptions (back-propagation to ADR-026)

> **ADR-026 original rule (quoted verbatim, §Decision and §Context intent #3):**
> "A new business rule: **a blocked item must NOT also be flagged stale** (blocked takes precedence), applied uniformly on every surface." Encoded as the clause `&& !item.isBlocked` inside `deriveStaleness`, so that "a blocked item over threshold stops rendering red."

**Superseding decision (this ADR, EXTENDS — does not revoke):** ADR-026's rule is **narrowed in scope from "staleness" to "TIME-IN-STATE staleness"**. The original premise was that the ONLY staleness source was time-in-state, so "blocked excludes stale" and "blocked excludes time-in-state stale" were the same statement. Epic 5074 introduces a SECOND staleness source — blocked DURATION — for which the exclusion is nonsensical (it fires *because* the item is blocked). Therefore:

- **PRESERVED**: a blocked item is NOT flagged stale by the TIME-IN-STATE trigger (`daysInState > StalenessThresholdDays`). ADR-026's `!isBlocked` guard stays — relocated into the time-in-state branch.
- **ADDED**: a blocked item IS flagged stale by the BLOCKED-DURATION trigger when `blockedDuration ≥ blockedStalenessThresholdDays > 0`, with a distinct reason.
- **NET**: "blocked takes precedence over time-in-state staleness" still holds (an item blocked in Review is not "11 days in Review" stale). "Blocked items are never stale" is RETIRED — it was an artifact of there being only one staleness source.

ADR-026 is **amended, not superseded** — its single-selector architecture (one home for all staleness logic) is upheld and EXTENDED; only the blocked-exclusion's scope changes.

---

## Decision

### 1. `deriveStaleness` returns a multi-reason result, not a bare boolean

The selector's contract changes from `→ boolean` to `→ StalenessResult`:

```typescript
type StalenessReason =
  | { kind: "time-in-state"; days: number; stateName: string }   // a staleness DRIVER
  | { kind: "blocked-duration"; days: number }                   // a staleness DRIVER
  | { kind: "context-time-in-state"; days: number; stateName: string };  // CONTEXT, not a driver

type StalenessResult = {
  isStale: boolean;            // true iff a DRIVER reason fires (OR over the two driver kinds)
  reasons: StalenessReason[];  // driver(s) + optional context; the item is stale ONCE
};
```

**UC-1 RESOLUTION — architecturally DECIDED here (reading (a), not deferred to product)**: a blocked item over its days-in-state threshold is NOT time-in-state-stale (ADR-026 preserved), but its days-in-state IS reported as a `context-time-in-state` entry alongside the `blocked-duration` driver. So slice-04 AC2's "lists both reasons" is honoured: the item is stale ONCE, driven by `blocked-duration`, with the days-in-state shown as context (`kind: "context-time-in-state"`). `isStale` is computed ONLY from driver kinds (`time-in-state`, `blocked-duration`) — a context entry never makes an item stale by itself. This makes the selector's contract unambiguous for the software-crafter and removes the DISTILL ambiguity (UC-1 was a clarification, now resolved; upstream-changes UC-1 updated to "resolved — context entry").

The selector input gains `blockedSince` and `blockedStalenessThresholdDays`:

```
deriveStaleness(
  item: { currentStateEnteredAt?, isBlocked, blockedSince?, currentStateName },
  stalenessThresholdDays, blockedStalenessThresholdDays, now
): StalenessResult
```

Logic (the architectural contract — software-crafter picks exact names at GREEN):

- **time-in-state reason**: `stalenessThresholdDays > 0 && !item.isBlocked && daysInState(currentStateEnteredAt, now) > stalenessThresholdDays` (ADR-026 preserved, including `> ` strict and the `!isBlocked` guard, now scoped here).
- **blocked-duration reason**: `blockedStalenessThresholdDays > 0 && item.isBlocked && item.blockedSince != null && blockedDays(blockedSince, now) >= blockedStalenessThresholdDays`.
- `isStale = reasons.length > 0` (OR). The item is rendered stale ONCE; the surface lists all `reasons` (D8 — both reasons when both fire, never double-counted).

### 2. Boundary convention: blocked-duration uses `≥` (OQ1 resolved)

OQ1 asked `>` vs `≥`. DECISION: blocked-duration staleness uses `≥` (an item blocked EXACTLY `blockedStalenessThresholdDays` days IS stale). Time-in-state KEEPS `>` (ADR-026 unchanged — at-threshold is NOT stale). Rationale: the two thresholds answer different questions and the slice-04 AC4 explicitly records `≥` for blocked, matching the journey intent ("blocked long ENOUGH"); they are independently tuned settings, so a deliberate boundary divergence is acceptable and documented (not an inconsistency to "fix"). Both boundaries are unit-tested at-threshold and one-over.

### 3. `blockedStalenessThresholdDays` is an additive settings field (twin of `StalenessThresholdDays`)

Added to `WorkTrackingSystemOptionsOwner` (Team + Portfolio), the settings DTO (`SettingsOwnerDtoBase`), and the FE settings model — exactly as `StalenessThresholdDays` is. Validated on the existing settings write (≥ 0; 0 = disabled). **Additive field on the existing contract ⇒ NO client version gate** (ADR-064/056 rule; contrast the new endpoint in ADR-069). Default 0 (disabled) so no team's behaviour changes on deploy.

### 4. All three ADR-026 surfaces consume the new result identically

The badge, the Stale Items widget, and the aging chart route through the one selector (ADR-026's single-home invariant upheld). They now read `result.isStale` for emphasis and `result.reasons` for the reason text. The widget COUNT is `inProgressItems.filter(i => deriveStaleness(...).isStale).length` — an item stale for both reasons counts ONCE (D8 — no double count, true by construction since the selector returns one result per item).

---

## Alternatives Considered

**Option A (chosen): one selector, multi-reason result, blocked-exclusion scoped to the time-in-state branch.**
- Pros: upholds ADR-026's single-home invariant; the OR + distinct-reasons + no-double-count fall out of one result object; the amendment is a scoped narrowing, not a rewrite.
- Cons: the selector's return type changes (boolean → object) — a typed ripple across three call sites. Bounded (three sites, type-checked).

**Option B: a SECOND selector `deriveBlockedStaleness` OR'd by each surface.**
- Cons: re-introduces the exact failure ADR-026 prevents — two staleness predicates each surface must OR and de-duplicate, three chances to forget the de-dup. Rejected — violates ADR-026's "single home" by construction.

**Option C: server-side `isStale` + reasons on `WorkItemDto`.**
- Cons: ADR-026 §Alternative C rejected this (breaks DDD-8: threshold edits must take effect on next render without a re-fetch). Both thresholds are per-owner and cheap to compute client-side. Rejected for the same reason ADR-026 rejected it.

---

## Consequences

**Positive**:
- ADR-026's single-selector architecture is preserved and extended; the blocked→stale linkage is honest (fires because blocked, distinct reason).
- OR + both-reasons + single-stale-once are structural properties of the one result object, not per-surface vigilance.
- Additive settings field ⇒ no client version gate.

**Negative**:
- Breaking change to the selector's return type (boolean → `StalenessResult`) rippling to three call sites + tests. Bounded and type-enforced.
- Two boundary conventions (`>` time-in-state, `≥` blocked) — a deliberate, documented divergence (OQ1), not a bug.

**Neutral**:
- Default threshold 0 ⇒ no behaviour change until a config-admin opts in.

---

## Earned Trust — probing the reconciliation

- **ADR-026-preservation probe**: a blocked item over the TIME-IN-STATE threshold returns NO time-in-state reason (the `!isBlocked` guard still holds in that branch) — the original ADR-026 test still passes.
- **New-trigger probe**: a blocked item with `blockedDays ≥ blockedStalenessThresholdDays` returns `isStale: true` with a `blocked-duration` reason and NO time-in-state reason.
- **Both-reasons probe (UC-1 resolved)**: an item blocked past `blockedStalenessThresholdDays` whose days-in-state also exceeds `StalenessThresholdDays` returns `isStale: true` with a `blocked-duration` DRIVER reason AND a `context-time-in-state` entry (NOT a `time-in-state` driver — the `!isBlocked` guard holds). The test asserts: one `isStale`, exactly one driver (`blocked-duration`), one context entry, the widget counts it once. This is slice-04 AC2 ("lists both reasons") satisfied by the driver+context split.
- **Boundary probe**: blocked exactly at threshold ⇒ stale (`≥`); in Review exactly at threshold ⇒ not stale (`>`).
- **Disabled probe**: `blockedStalenessThresholdDays = 0` ⇒ no blocked-duration reason ever.
- **Single-count probe**: the widget count treats a one-item-two-reasons item as one.

**UC-1 RESOLVED (slice-04 AC2)**: AC2's "blocked 12 days AND 13 days in Review, both reasons listed" cannot mean two staleness DRIVERS (ADR-026 preserved makes a blocked item NOT time-in-state-stale). Resolved (above, reading (a)): the item is stale ONCE, driven by `blocked-duration`, with days-in-state reported as a `context-time-in-state` entry — both pieces of information are shown, only one is a stale driver. This is an architectural decision recorded here, NOT deferred; `design/upstream-changes.md` UC-1 is updated to "resolved". The acceptance-designer reflects the driver/context distinction in the AC2 Gherkin at DISTILL.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| ALL staleness (both triggers) routes through the one `deriveStaleness` selector | Code-review gate w/ this ADR + ADR-026 as canon (TS has no method-presence enforcement — same limitation as ADR-026); Vitest: widget count == badge red-state for the same input |
| Time-in-state still excludes blocked (ADR-026 preserved) | Vitest: blocked item over time-in-state threshold ⇒ no time-in-state reason |
| Blocked-duration uses `≥`; time-in-state uses `>` | Vitest at-threshold + one-over for each trigger |
| Item stale once; reasons listed; no double count | Vitest: two-reason item ⇒ one `isStale`, `reasons.length` correct; widget counts it once |
| `blockedStalenessThresholdDays` additive ⇒ no client gate | Clients handoff note: additive settings field, no registry entry |

**Exhaustive selector-use audit (mandate)**: the return-type widening (`boolean → StalenessResult`) touches every `deriveStaleness` caller. Grep the ENTIRE Frontend for `deriveStaleness` call sites and verify EVERY one consumes `StalenessResult.isStale` + `reasons[]`, NOT a residual boolean read. Code-review gate: no merge without exhaustive-audit evidence (the list of call sites with their consumption confirmed). A single missed call site silently reading the object as truthy is the exact failure this audit exists to catch.

**Earned-Trust probe (three call sites)**: all three ADR-026 surfaces — badge, Stale Items widget, aging chart — render `isStale` + `reasons`. Vitest asserts that for an input with `isStale = true`, all three emit identical red-state emphasis (one stale-once rendering, driver + context reasons shown), so no surface diverges in how a stale item is emphasised.

**ADR-026 backward-compat audit (mandate)**: ADR-026 encoded "a blocked item is never stale". Audit production code for latent assumptions built on that retired premise — notifications, telemetry gates, integrations that branch on blocked-vs-stale. Record findings in `distill/upstream-changes.md`. Any path assuming blocked items cannot be stale MUST be updated to allow blocked-duration staleness BEFORE this amendment lands (a stale-blocked item must not be silently dropped by a downstream consumer that still believes the old exclusion).

---

## Cross-feature impact

- **ADR-026**: AMENDED — blocked-exclusion narrowed to the time-in-state branch; single-selector invariant upheld and extended; return type widened to `StalenessResult`. ADR-026's status stays Accepted with a forward reference to this ADR.
- **Epic 4144 (time-in-state-and-staleness)**: its selector is the one extended here; its surfaces (badge, widget, aging chart) consume the new result.
- ADR-067/068: blocked-duration derives from `IsBlocked` + `blockedSince` (single sources).
- Lighthouse-Clients: additive threshold field ⇒ no gate (ADR-072).
