# ADR-104: Feature Blocked Capture Seam — Pre-Update Verdict in `RefreshFeatures`, New `FeatureBlocked`/`FeatureUnblocked` Events, Departure Sweep Closes Abandoned Spells

**Status**: **Accepted** (2026-07-19 — Morgan, interaction mode PROPOSE; confirmed by user 2026-07-20). Depends on ADR-103 (Accepted); the event payloads and the sweep key both carry `PortfolioId` from that decision.
**Date**: 2026-07-19
**Feature**: portfolio-blocked-history (Story #5524, slice 02)
**Decider**: Morgan (Solution Architect), confirmed by user 2026-07-20
**Relationship to prior ADRs**: applies ADR-068's enter/leave-on-the-bus pattern to the `Feature` aggregate and ADR-017's capture-dispatch idiom to the portfolio refresh path. Consumes ADR-102's entity and ADR-103's keying. Governed by ADR-027 / Epic 5121 (domain-event bus). Resolves DESIGN DDD-3, DDD-4 and DDD-5.

---

## Context

DISCUSS carried one genuine unknown into DESIGN, as slice 02's learning hypothesis: *can the portfolio refresh path observe a feature's pre-update blocked state, the way `SyncedItem.WasBlockedBeforeSync` does for work items?*

The team path captures the prior verdict **before** mutating the persisted entity:

```csharp
// WorkItemService.cs:73-80
var existingItem = storedWorkItems.SingleOrDefault(wi => wi.ReferenceId == item.ReferenceId);
var wasBlocked = WasBlocked(team, existingItem);          // :75 — reads IsBlocked on the UNMUTATED entity
var persistedItem = SyncWorkItem(item, existingItem);     // :76 — existingItem.Update(item) at :236
...
itemsWithTransitions.Add(new SyncedItem(persistedItem, syncedTransitions, wasBlocked));
```

The edge is then detected at `:121-129` and raised as `WorkItemBlocked` / `WorkItemUnblocked`.

The feature path has no equivalent. `RefreshFeatures:485` calls `AddOrUpdateFeature(feature)`, which performs the lookup *inside itself* at `:530` and immediately calls `featureFromDatabase.Update(feature)` at `:540`, destroying the prior blocked state before any caller can read it.

**DESIGN finding: the hypothesis is very likely confirmed, and cheaply.** The lookup that must be hoisted is one line (`:530`), inside a method called from one place in the capture-relevant path (`:485`). Hoisting it into the caller is a change of roughly ten lines, well inside the one-day disproof budget. The seam is not the risk DISCUSS feared.

Two structural facts constrain the design:

- **`feature.Id` is 0 until saved.** `SyncFeatureStateTransitions` already handles this by running in a **second pass after `featureRepository.Save()`** (`RefreshFeatures:494-501`). Anything keyed on `feature.Id` must do the same.
- **The domain-event dispatcher swallows handler exceptions.** A failing capture handler is silent; the only symptom is a missing spell. No mechanism may rely on an exception surfacing.

---

## Decision

### 1. Capture the pre-update verdict by hoisting the lookup — a `SyncedFeature` record mirroring `SyncedItem`

`AddOrUpdateFeature` gains an `existing` parameter (or returns the pair); `RefreshFeatures` performs the `ReferenceId` lookup, computes the verdict against the **unmutated** entity, then updates:

```
foreach (feature from connector):
    existing    = featureRepository.GetByPredicate(f => f.ReferenceId == feature.ReferenceId)
    wasBlocked  = existing != null && blockedItemService.IsBlocked(existing, portfolio)   // ADR-103 grain
    persisted   = AddOrUpdateFeature(feature, existing)
    AddProjectToFeature(persisted, portfolio)
    syncedFeatures.Add(new SyncedFeature(persisted, syncedTransitions, wasBlocked))
```

`private sealed record SyncedFeature(Feature PersistedFeature, IReadOnlyList<WorkItemStateTransition> SyncedTransitions, bool WasBlockedBeforeSync)` — the direct twin of `SyncedItem` (`WorkItemService.cs:197`).

Edge detection and event collection run in the **existing second pass** at `:496-499`, after `featureRepository.Save()` at `:494`, where `persisted.Id` is non-zero:

```
isBlockedNow = blockedItemService.IsBlocked(persisted, portfolio)
!wasBlocked && isBlockedNow  → FeatureBlocked(persisted.Id, portfolio.Id, reason)
 wasBlocked && !isBlockedNow → FeatureUnblocked(persisted.Id, portfolio.Id)
```

Events are collected into a list and published at the end of `RefreshFeatures` via `PublishDomainEvents`, mirroring `RefreshWorkItems:101`.

**Ordering note**: these publish *before* `PortfolioUpdater:73` raises `PortfolioFeaturesRefreshed`, which is what `BlockedCountSnapshotRecordingHandler` consumes. So spells are written before today's snapshot is recorded, and the two agree for today's bar — which is what makes the ADR-099 reconciliation guard quiet on a healthy system.

**First observation** is preserved by construction: a feature already blocked when capture begins has `existing != null` and `wasBlocked = true`, so no edge fires, no spell opens, `blockedSince` is null and the UI renders "—" with the establishing-baseline tooltip. This is ADR-068 §3's honest forward-only behaviour and satisfies US-02 AC4.

**Capture is confined to `RefreshFeatures`.** `RefreshParentFeatures:547-568` also calls `AddOrUpdateFeature` but is *not* a capture site — parent features are not added to `portfolio.Features` by `UpdateFeatures` (`:492`). DISTILL must pin with a test whether `GetBlockedEligibleFeaturesForPortfolio` includes parent features; if it does, the snapshot count and the reconstructed membership will diverge and the ADR-099 guard will fire. See Open Question OQ-1.

### 2. New `FeatureBlocked` / `FeatureUnblocked` events — not generalised `WorkItemBlocked`

```csharp
public record FeatureBlocked(int FeatureId, int PortfolioId, string Reason) : IDomainEvent;
public record FeatureUnblocked(int FeatureId, int PortfolioId) : IDomainEvent;
```

`Reason` follows `ResolveBlockReason` (`WorkItemService.cs:152-158`): the feature's current state, as human-readable text only. The blocked *decision* stays with `IBlockedItemService`.

Handled by a new `FeatureBlockedTransitionCaptureHandler : IDomainEventHandler<FeatureBlocked>` and `FeatureBlockedTransitionCloseHandler : IDomainEventHandler<FeatureUnblocked>`, structurally identical to `WorkItemBlockedTransitionCaptureHandler` / `CloseHandler`, keyed on `(FeatureId, PortfolioId)` and idempotent (open-spell check before insert, mirroring `:15-24`). Registered in `Program.cs` alongside the existing handlers (~`:1053-1060`).

**Contract shape (Mandate-12)**: both are **bounded-change** contracts. Universe: the `FeatureBlockedTransition` rows for exactly one `(FeatureId, PortfolioId)` pair. Delta: open or close exactly one spell. No cross-feature and no cross-portfolio mutation is representable, because the repository query signature requires the portfolio (ADR-102).

### 3. Lifecycle — three closers, only one of which needs code

| Event | Closer |
|---|---|
| Feature deleted (incl. `orphan-feature-cleanup`) | FK cascade on `FeatureId` (ADR-102). No code. |
| Portfolio deleted | FK cascade on `PortfolioId` (ADR-102). No code. |
| Feature leaves the portfolio (connector no longer returns it) | **Departure sweep** — see below. |

The third is the real gap. `RefreshFeatures` only visits features the connector returns. A feature that drops out is never evaluated again, so its open spell never closes: it reads blocked **forever**, on every future date, in both the count and the drill-through. The team path already has the departure concept (`RefreshWorkItems:83-87` removes stored items the connector no longer returns); the feature path needs the spell half of it.

**Decision — close-on-departure sweep**, at the end of `RefreshFeatures`, after the save:

```
refreshedIds = syncedFeatures.Select(f => f.PersistedFeature.Id)
abandoned    = repo.GetOpenSpells(portfolio.Id).Where(s => !refreshedIds.Contains(s.FeatureId))
abandoned.ForEach(s => s.LeftAt = refreshTime)
```

Historic truth is preserved — the spell still covers the interval it genuinely covered — while the false forward claim stops at the moment we stopped observing the feature.

**Guard**: skip the sweep entirely when `refreshedIds` is empty. A transient connector failure returning zero features would otherwise close every open spell in the portfolio, silently, and the dispatcher would surface nothing. A portfolio that genuinely has zero features has no open spells to close, so the guard costs nothing.

---

## Alternatives Considered

**Seam — Option A (chosen): hoist the lookup, `SyncedFeature` mirrors `SyncedItem`.**

- Pros: exact twin of the proven team seam, so one idiom to learn and a single parity test matrix; preserves first-observation "—" (US-02 AC4) with no extra machinery; ~10 lines.
- Cons: `AddOrUpdateFeature`'s signature changes, touching `RefreshParentFeatures` as a caller; the in-memory verdict is lost if the dispatcher swallows a handler error (see Consequences).

**Seam — Option B: derive `wasBlocked` from the spell store itself (an open spell *is* the prior verdict).**

- Pros: the in-place-mutation problem evaporates — the prior verdict lives in a table `Update()` never touches; no signature change; **self-healing**, because a swallowed handler error is re-derived and re-emitted on the next refresh, which is a genuine advantage given that the dispatcher hides failures.
- Cons, decisive: it cannot distinguish "never observed by capture" from "observed and not blocked". A feature already blocked at release would therefore have a spell opened with `EnteredAt = now`, rendering "blocked 0d" — fabricated history, contradicting ADR-068 §3 and failing US-02 AC4 outright. Restoring AC4 requires a separate per-`(feature, portfolio)` baseline marker, which is more machinery than Option A saves.
- **Rejected**, but its self-healing property is worth revisiting: a bounded reconciliation sweep (close open spells whose feature no longer matches the rule set, even when no edge was detected) would recover most of it on top of Option A. Deferred as OQ-2 rather than bundled into slice 02, which already carries the story's uncertainty.

**Seam — Option C: derive blocked spells from `FeatureStateTransition` history.**

This was DISCUSS's named fallback if the hypothesis was disproved. Since the hypothesis holds, it is moot — and it would not have worked: blocked rule sets match on tags and additional fields as well as state, so a state stream cannot represent a tag-driven block. Blocked ⊥ state (README L1); ADR-068 rejected the same idea for work items.

**Seam — Option D: persist a `WasBlockedAtLastRefresh` flag, mirroring `WorkItem.WasStaleAtLastSync` (`:139-149`).**

- Pros: an existing precedent in this very file; immune to in-place mutation.
- Cons: under ADR-103 the flag is a property of the *(feature, portfolio)* relationship, not of the feature, so it needs a payload on the many-to-many join — an explicit join entity and a much larger migration than the whole rest of the slice.
- **Rejected** on migration cost.

**Events — Option E: generalise `WorkItemBlocked` / `WorkItemUnblocked` with an owner discriminator instead of adding a feature pair.**

- Pros: two fewer record types.
- Cons, decisive: `WorkItemBlockedTransitionCaptureHandler` is registered as `IDomainEventHandler<WorkItemBlocked>` and writes `WorkItemId` unconditionally (`:26-31`). A generalised event would deliver feature ids to it and reproduce, at runtime and for every real customer, the exact identity-collision defect slice 01 removes from the demo path. The existing handler would need a discriminator guard whose omission is silent. ADR-068 established the pair as work-item-specific; the events are also positionally registered by closed generic type in `Program.cs`.
- **Rejected.** Two record types is a small price for making the collision unrepresentable.

**Lifecycle — Option F: leave abandoned spells open and filter at read time** (as `TeamMetricsController:522-523` intersects reconstructed ids with the team's own items).

- Pros: no sweep; historic reads stay correct because the intersection is against the *current* member set.
- Cons: intersecting against the current set is what *masked* the phantom in the drill-through while leaving the count wrong — the same asymmetry recurs here. A departed feature still reads blocked for today wherever the intersection is not applied, and the count and the list diverge again. Read-time filtering also cannot express "blocked until it left", only "blocked or not, now".
- **Rejected** — but the read-side intersection is still applied, defensively, on top of the sweep.

---

## Consequences

**Positive**:

- Symmetric with the team capture path: same edge shape, same event-on-the-bus pattern, same second-pass-after-save discipline. The Team-vs-Portfolio parity KPI is testable as one matrix over both controllers.
- Spells are written before `PortfolioFeaturesRefreshed`, so the snapshot and the reconstruction agree for today and the ADR-099 guard stays quiet on a healthy system.
- The departure sweep makes "blocked forever because we stopped looking" non-representable, and it is the same mechanism that covers the orphan case before cleanup runs.
- Handler mutation universes are bounded to one `(feature, portfolio)` pair.

**Negative**:

- **No self-healing.** The verdict is captured in memory for the duration of one refresh; if the dispatcher swallows a handler failure, that edge is lost permanently and the spell is simply missing. The only detector is the ADR-099 reconciliation warning, which compares counts and will catch it the next day. This is a real gap, inherited from the team path, and OQ-2 is where it gets addressed.
- `AddOrUpdateFeature`'s signature change touches `RefreshParentFeatures`.
- A feature in N portfolios is evaluated once per portfolio refresh — N `IsBlocked` calls per cycle instead of one. Negligible: `IsBlocked` is a pure rule-set match over an already-loaded entity.
- The sweep adds one indexed query per portfolio refresh.

**Neutral**:

- No contract change; no client version gate.
- Two new event records on the existing bus; no dispatcher change.

---

## Earned Trust — probing the capture

Every probe asserts **rows present or absent**, never absence-of-throw: the dispatcher swallows handler exceptions, so "no exception" proves nothing.

- **Enter probe**: a feature starts matching the portfolio's rule set between refresh N−1 and N → exactly one open spell for `(featureId, portfolioId)` with `EnteredAt` at refresh time; `blockedSince` populated on `GET /portfolios/{id}/metrics/wip`. (US-02 AC1/AC3.)
- **Leave probe**: it stops matching → the open spell closes with `LeftAt` set; `blockedSince` becomes null. (US-02 AC2.)
- **Re-block probe**: block → unblock → block opens a **new** spell; the badge reads the new spell, not the old one. (US-02 AC2.)
- **First-observation probe**: a feature already blocked at the first refresh after release opens **no** spell and renders "—". (US-02 AC4.)
- **Idempotency probe**: replaying `FeatureBlocked` with an open spell present is a no-op — no duplicate row.
- **Id-timing probe**: a feature seen for the first time (Id = 0 before save) still gets its spell — asserts the second-pass ordering, the failure mode `SyncFeatureStateTransitions` already had to solve.
- **Departure probe**: a blocked feature disappears from the connector's result → its spell closes at that refresh; `blockedItemsAtDate` for a *later* date excludes it; for a date inside the spell it still includes it.
- **Empty-result probe** (the environment-lies probe): the connector returns zero features for a portfolio that has open spells → **no spell is closed**. This is the specific lie the sweep guard exists for.
- **Multi-portfolio probe**: see ADR-103's divergent-rule-set probe — the same refresh must not touch the other portfolio's spells.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Spells are opened/closed only by the two handlers | ArchUnitNET: no type outside `Services.Implementation.DomainEvents` and the demo backfill may write `FeatureBlockedTransition` |
| Feature events carry `PortfolioId` | Compile-time — the record's positional parameter |
| `WorkItemBlocked` handlers never receive feature ids | Type system — separate closed generic registrations; plus the ADR-102 repository invariant test |
| Capture runs after `featureRepository.Save()` | NUnit: the id-timing probe (a first-seen feature gets a spell) |
| The departure sweep is skipped on an empty refresh | NUnit: the empty-result probe |
| Edge detection reads the pre-update entity | NUnit: a feature that becomes blocked in the same refresh in which its fields change still raises exactly one `FeatureBlocked` |

---

## Open Questions (deferred to DISTILL / DELIVER)

- **OQ-1** — Does `GetBlockedEligibleFeaturesForPortfolio` include parent features (`IsParentFeature = true`)? If it does, the snapshot counts a population the capture seam never visits, and the ADR-099 guard will fire on every reconciliation. DISTILL must pin the eligible population with a test and align the capture site to it. Not resolved here because it depends on `PortfolioMetricsService` internals that were not read during this DESIGN pass.
- **OQ-2** — Should the capture path gain a reconciliation sweep (close open spells whose feature no longer matches the current rule set, independent of edge detection) to recover Option B's self-healing property? Attractive, and it would also cover swallowed handler failures on the team path. Deliberately out of slice 02.
- **OQ-3** — `TeamMetricsController:524` passes `null` for `blockedSince` in the historic drill-through while `:152` populates it in the historic `wip` read. The portfolio path mirrors the team path for parity; whether both should populate it is a separate, shared question.
