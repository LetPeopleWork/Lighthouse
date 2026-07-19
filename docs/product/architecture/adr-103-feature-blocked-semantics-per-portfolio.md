# ADR-103: A Feature Is Blocked **Per Portfolio**, Not Across Any Portfolio — Spells Keyed `(FeatureId, PortfolioId)`, Any-Portfolio Retained as a Read Projection

**Status**: **PROPOSED** (2026-07-19 — Morgan, interaction mode PROPOSE). **Awaiting user confirmation.** This ADR resolves DISCUSS D8 / journey D12, which DISCUSS deliberately left open as a domain-semantics fork. Slice 02 must not start until this is Accepted — the keying cannot be changed after capture begins without a data migration.
**Date**: 2026-07-19
**Feature**: portfolio-blocked-history (Story #5524, slice 02)
**Decider**: Morgan (Solution Architect), pending user confirmation
**Relationship to prior ADRs**: upholds ADR-067 (`IBlockedItemService` is the single blocked authority) by removing its only surviving competitor. Determines the key of the entity created in ADR-102 and the event shape in ADR-104. Keeps ADR-069's `BlockedCountSnapshot` grain and ADR-099's reconciliation guard consistent. Resolves DESIGN DDD-2, DDD-4 and DDD-7.

---

## Context

A `Feature` can belong to many `Portfolio`s — `Feature.Portfolios` is a `List<Portfolio>` (`Models/Feature.cs:41`), populated by `WorkItemService.AddProjectToFeature` (`:570-577`), and `AddOrUpdateFeature` (`:530`) looks features up by `ReferenceId` **globally**, so two portfolios genuinely share one `Feature` row. The blocked rule set lives on the Portfolio (`Portfolio.BlockedRuleSetJson`), so two portfolios can disagree about whether the same feature is blocked.

The codebase already contradicts itself about what that means:

| Call site | Semantics |
|---|---|
| `FeatureDto.FeatureIsBlocked` (`FeatureDto.cs:82`) | **Any-portfolio** — `feature.Portfolios.Any(p => IsBlockedByPortfolioRuleSet(feature, p))`, evaluated inline with a private static `RuleEvaluator<Feature>` (`:71`), bypassing `IBlockedItemService` entirely |
| `BlockedCountSnapshotRecordingHandler:72` | **Per-portfolio** — `blockedItemService.IsBlocked(feature, portfolio)` |
| `PortfolioMetricsController.GetBlockedItemsAtDate:494` (live branch) | **Per-portfolio** |
| `DemoBlockedHistoryBackfillHandler:88` | **Per-portfolio** |

So a feature in Portfolio A (rule matches) and Portfolio B (rule does not) is rendered **blocked** on B's feature list but is **absent** from B's blocked count and B's drill-through. This divergence pre-dates Story #5524. Capture cannot be built without resolving it, because a spell must be keyed either `FeatureId` or `(FeatureId, PortfolioId)`.

Two further facts constrain the answer.

**The port signature is already per-portfolio.** `IBlockedItemService.IsBlocked(Feature item, Portfolio owner)` — ADR-067 defined blocked as a function of *(item, owner)*. Any-portfolio is not a competing definition; it is a call site that lacked the owner and papered over the gap with `Any`. The comment at `FeatureDto.cs:75-81` says as much, naming the fix as a known bounded ADR-067 slice-01 follow-up.

**The capture seam is already per-portfolio.** `WorkItemService.RefreshFeatures(Portfolio portfolio)` (`:476`) runs once per portfolio, driven by `PortfolioUpdater:73`. Whatever is captured there is naturally scoped to one portfolio's rule set.

---

## Decision

**Per-portfolio is the definition. Any-portfolio survives only as an explicitly named read projection for scope-free surfaces.**

### 1. The spell key is `(FeatureId, PortfolioId)`

`FeatureBlockedTransition` (ADR-102) carries `PortfolioId`. At most one **open** spell (`LeftAt == null`) per `(FeatureId, PortfolioId)` pair. A feature in two portfolios can hold two independent open spells with different `EnteredAt` values — which is the truthful representation: it was blocked by A's rules from one date and by B's from another.

### 2. Portfolio-scoped surfaces answer per-portfolio

`GET /portfolios/{id}/metrics/wip`, `GET /portfolios/{id}/metrics/blockedItemsAtDate` and `blockedCountHistory` all resolve blocked against **that portfolio's** spells and rule set. This makes the DTO agree with `BlockedCountSnapshot` for the first time.

**This is an observable behaviour change.** A feature blocked only by Portfolio A's rules stops rendering as blocked when viewed from Portfolio B's page. That is the D12 correction, and it is the point: today B's feature list and B's blocked count disagree, and B's admin configures a rule set whose answer the list ignores.

### 3. Scope-free surfaces use an Any-portfolio projection, and say so

Three `FeatureDto` build sites have no portfolio in scope:

- `TeamMetricsController:113` (a team's features span portfolios)
- `FeaturesController:97` (cross-portfolio list, filtered by `readablePortfolioIds`)
- `DeliveryRulesController:58` (delivery-rule preview)

These derive `isBlocked = feature has ANY open spell` and `blockedSince = MIN(EnteredAt)` over open spells — the *longest-standing* blocker, which is also what the max-blocked-age RAG chip wants. Their observable behaviour is unchanged from today. The distinction is that Any-portfolio is now a documented **projection over a per-portfolio store**, not a second definition of blocked.

### 4. `FeatureDto` stops computing blocked and closes the ADR-067 follow-up (DDD-7)

`FeatureIsBlocked`, `IsBlockedByPortfolioRuleSet`, the private static `RuleEvaluator<Feature>` and `FeatureFieldProvider` (`FeatureDto.cs:65-98`) are **deleted**. `isBlocked` and `blockedSince` become constructor arguments supplied by the caller, which resolves them through `IBlockedItemService` (portfolio-scoped sites) or the spell projection (scope-free sites). `PortfolioMetricsController` (`:24`) and `TeamMetricsController` already inject `IBlockedItemService`.

This lands as a **separate refactor commit ordered before** the behavioural commit in slice 02, per the repo's refactor-commits-separate rule. It is not optional decoration: under §2 a surviving Any-computation inside the DTO would silently override the injected per-portfolio verdict on exactly the surfaces this story fixes. Two definitions of blocked is what ADR-067 exists to forbid.

*Fallback if the refactor proves larger than the four call sites suggest*: add optional `isBlocked` / `blockedSince` ctor parameters that take precedence when supplied, leave the static computation as the no-argument fallback, and file the deletion as a follow-up. Record the fallback explicitly if taken — do not take it silently.

---

## Alternatives Considered

**Option A — Any-portfolio is the definition; spells keyed by `FeatureId` alone.**

Change `BlockedCountSnapshotRecordingHandler:72`, the live drill-through branch and the demo backfill to Any-semantics; `FeatureDto` keeps its current logic.

- Pros: simplest key; smallest entity; no change to `FeatureDto`; one spell per feature regardless of portfolio count.
- Cons, in order of severity:
  1. **A portfolio's blocked count would count features its own rule set does not match.** A config-admin (Carlos) configures `BlockedRuleSet` on Portfolio B and gets a count driven partly by Portfolio A's configuration, with no way to see or change it. Indefensible for the persona whose whole job on this surface is that setting.
  2. **It regresses `BlockedCountSnapshot`.** That store is per-portfolio and has been since ADR-069. Switching it to Any-semantics changes the meaning of already-recorded history without a migration, so the over-time chart would splice two definitions at the release boundary.
  3. **It poisons the ADR-099 reconciliation guard.** `PortfolioMetricsController:513` compares reconstructed membership against the snapshot and logs a divergence warning. Under Any-semantics-for-one-and-per-portfolio-for-the-other, or under Any-for-both-but-history-recorded-per-portfolio, the guard fires permanently on a known-benign cause — degrading a shipped safety net into noise. US-04 AC4 requires the guard to be *called* on this path, which only helps if it is quiet when things are correct.
  4. **It creates a write-contention hazard at the capture seam.** `RefreshFeatures` is per-portfolio, so a feature in two portfolios is refreshed by two independent cycles. With one shared spell row, cycle B's evaluation of A's rule set can close a spell cycle A just opened, and the edge is not attributable to the refresh that observed it. Per-portfolio keying gives each cycle a private row: the handler's declared mutation set is bounded to `(this feature, this portfolio)`, so "a refresh wrote to another portfolio's spell" is non-representable.
- **Rejected.** Cheapest key, most expensive semantics.

**Option B (chosen) — per-portfolio definition, `(FeatureId, PortfolioId)` key, Any as a projection.**

- Pros: agrees with the port signature ADR-067 already chose; agrees with 3 of the 4 existing call sites; agrees with `BlockedCountSnapshot`'s existing grain, so no historical re-interpretation; keeps the ADR-099 guard meaningful; the capture seam is already per-portfolio so the keying costs nothing to produce; each refresh cycle mutates only its own rows; the second FK (`PortfolioId → Portfolios`, cascade) disposes of spells when a portfolio is deleted for free.
- Cons: one extra column and index; a feature in N portfolios stores up to N open spells; scope-free surfaces need an explicit projection rule; the `FeatureDto` refactor becomes mandatory rather than optional; **one observable behaviour change** (§2) that must be called out in the release notes.

**Option C — per-portfolio definition, but key spells by `FeatureId` and re-derive the portfolio at read time from `Feature.Portfolios`.**

- Pros: no extra column.
- Cons: it cannot represent the disagreement it is supposed to model — a single spell has one `EnteredAt`, but the feature may have started matching A's rules in May and B's in June. Re-derivation at read time evaluates *today's* rule sets over a *past* interval, which is precisely the "today's rules wearing last month's date" defect US-03 exists to fix. Self-defeating.
- **Rejected.**

---

## Consequences

**Positive**:

- One definition of blocked for features, and it is `IBlockedItemService.IsBlocked(feature, portfolio)` — the signature ADR-067 chose. The last competing evaluation path in the codebase is deleted.
- `FeatureDto`, `BlockedCountSnapshot`, the drill-through and the demo backfill agree for the first time. The parity KPI (Team vs Portfolio agreement on the same range) becomes achievable rather than approximately achievable.
- The ADR-099 reconciliation guard becomes a real signal on the portfolio path: any divergence it logs now indicates a genuine capture gap.
- Effect isolation: each refresh cycle's mutation universe is exactly one `(feature, portfolio)` row set.

**Negative**:

- **Observable behaviour change**, needs a release-notes line: a feature blocked only by another portfolio's rules no longer renders blocked on this portfolio's page. Some users will read this as features "disappearing" from the blocked list. The honest framing is that the list now matches the count and the rule set the admin configured.
- Storage grows with portfolio membership. Bounded in practice — features typically belong to one portfolio — and each spell is four columns.
- The `FeatureDto` refactor touches four controllers before slice 02's behavioural work begins.

**Neutral**:

- No contract change. `isBlocked` and `blockedSince` already exist on `WorkItemDto`; their *values* change for features, their shape does not. No client version gate (ADR-072 / ADR-062 additive rule).
- Scope-free `FeatureDto` sites keep today's observable behaviour, so the team feature list and the cross-portfolio feature list are unaffected.

---

## Earned Trust — probing the semantics

- **Divergent-rule-set probe**: one feature, two portfolios, blocked rule matches in A only. Assert: A's `wip` reports `isBlocked = true` with a `blockedSince`; **B's reports `false`**; A's `blockedCountHistory` counts it, B's does not; A's `blockedItemsAtDate` contains it, B's does not. This single scenario is the whole ADR; it must exist as an integration test.
- **Independent-spell probe**: the same feature blocked in A from T1 and in B from T2 holds two open spells with distinct `EnteredAt`; unblocking in A closes A's spell and leaves B's open.
- **Projection probe**: with only A's spell open, `GET /teams/{id}/metrics/features` and `GET /features` report the feature blocked with `blockedSince = A`'s `EnteredAt` — Any-projection preserved.
- **Longest-blocker probe**: two open spells → the scope-free projection reports `MIN(EnteredAt)`, so the RAG chip colours off the oldest blocker.
- **Single-definition probe**: assert no type outside `BlockedItemService` constructs a `RuleEvaluator<Feature>` for a blocked decision (ArchUnitNET) — the mechanical proof that `FeatureDto`'s competitor is gone and does not return.
- **Reconciliation-quiet probe**: after a refresh, `blockedItemsAtDate` for today reconstructs a count equal to the recorded `BlockedCountSnapshot`, so `ReconcileReconstructedCountWithSnapshot` logs nothing. A divergence here means capture is genuinely broken.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| At most one open spell per `(FeatureId, PortfolioId)` | NUnit on the capture handler: a second enter with an open spell is a no-op (mirrors `WorkItemBlockedTransitionCaptureHandler:15-24`) |
| `FeatureDto` does not compute blocked | ArchUnitNET: `FeatureDto` may not reference `RuleEvaluator<>`, `FeatureFieldProvider` or `WorkItemRuleSet` |
| Blocked decisions route through `IBlockedItemService` | ArchUnitNET: only `BlockedItemService` may construct `RuleEvaluator<Feature>` for a blocked verdict |
| Portfolio-scoped endpoints never use the Any-projection | NUnit: the divergent-rule-set probe above |
| `BlockedCountSnapshot` grain unchanged | Existing tests; no migration touches it |

---

## Cross-feature impact

- ADR-067: closes the feature-surface follow-up its own `FeatureDto.cs:75-81` comment records.
- ADR-069: `BlockedCountSnapshot` semantics confirmed per-portfolio and left unchanged — no re-interpretation of recorded history.
- ADR-099: reconciliation guard becomes meaningful on the portfolio path (US-04 AC4).
- ADR-102: fixes `PortfolioId` as part of the spell key and adds the second cascade FK.
- ADR-104: the `FeatureBlocked` / `FeatureUnblocked` event payloads carry `PortfolioId` because of this decision.
- Lighthouse-Clients: no version gate (values change, shape does not).
- Release notes: the §2 behaviour change needs a customer-facing line.
