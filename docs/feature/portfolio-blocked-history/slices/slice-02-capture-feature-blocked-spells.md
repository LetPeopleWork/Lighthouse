# Slice 02 — Capture feature blocked spells and surface blocked duration

**Story**: US-02 | **ADO**: #5524 | **Effort**: ~1 crafter-day

## Goal

Record blocked enter/leave spells for Features on the portfolio refresh path, and populate `blockedSince` on `FeatureDto` — which lights up the `blocked Nd` badge, the max-blocked-age RAG chip and blocked→stale for portfolios in one move.

## Why second

Carries the feature's **only real uncertainty** (below). Everything downstream — slices 03, 04, 05 — is invalidated if this seam does not work, so it must fail early and cheaply. It is also the widest payoff: three parity-matrix rows close at once because the frontend is already shared (C4).

## IN scope

- Feature blocked/unblocked domain events raised from the portfolio refresh path, mirroring `WorkItemService.CollectDomainEvents:120-129`.
- Capture + close handler pair, modelled on `WorkItemBlockedTransitionCaptureHandler` / `CloseHandler`.
- Persistence in the **feature keyspace** per D3 — exact shape is DESIGN's call (separate entity vs discriminator).
- EF migration via the `CreateMigration` script, all providers, expand-only.
- `FeatureDto` passes the real `blockedSince` instead of the hardcoded `null` at `FeatureDto.cs:18`.
- **Precursor commit** (not a separate slice): entity + migration + repository land first, then the behavioural commit. Per the slice-composition gate, infrastructure precedes the slice, it does not become one.

## OUT of scope

- Historic as-of reads — slice 03.
- Drill-through — slice 04.
- Demo backfill — slice 05.
- Any frontend change. If one appears necessary, stop: it means a portfolio-specific branch is being introduced and US-02 AC6 forbids it.
- Resolving D8 — DESIGN delivers the answer; this slice implements it.

## Learning hypothesis

**Hypothesis**: the portfolio refresh path can observe a feature's pre-update blocked state, the way `SyncedItem.WasBlockedBeforeSync` does for team items.

The concern is concrete: `AddOrUpdateFeature` (`WorkItemService.cs:528`) calls `featureFromDatabase.Update(feature)` **in place**, destroying the prior blocked state before any edge-detection can read it. `RefreshFeatures` has no `SyncedItem` equivalent.

- **Confirmed if** a feature that starts matching the blocked rule set opens exactly one spell, and one that stops matching closes it, across two consecutive refreshes.
- **Disproved if** pre-update state cannot be captured without restructuring `RefreshFeatures` beyond a one-day change. Then event capture is the wrong seam for features and DESIGN must reconsider — most plausibly deriving spells from `FeatureStateTransition` history instead.

**If disproved, stop and escalate. Do not extend the slice to make it fit.**

## Acceptance criteria

See US-02 AC1–AC6 in `../feature-delta.md`. Summary: spell opens on block (AC1), closes on unblock with re-block opening a new spell (AC2), `blockedSince` served and rendered (AC3), first-observation shows `—` (AC4), blocked→stale and RAG honour the threshold including `0` = disabled (AC5), zero portfolio-specific frontend branches (AC6).

## Dependencies

- Slice 01 (the keyspace must be uncontaminated before it is populated).
- DESIGN's answers on D3 mechanics, D8 semantics, and the seam.

## Reference class

Epic-5074 slice-02 built the equivalent team-side capture (entity + repo + 2 handlers + DTO field) in roughly one day. This mirrors it against a different aggregate with one novel unknown, hence same estimate rather than less.

## Risks

- **The dispatcher swallows handler errors.** A failing capture handler is silent — the symptom is missing spells, never an exception. Assert rows exist; never assert absence-of-throw.
- **A feature in multiple portfolios** is exactly where D8 bites. Do not start until D8 is answered, or the keying will have to be redone.
- **Orphaned features** leave open spells dangling. Flagged to DESIGN; if unanswered when the slice starts, ask rather than improvise.
- Migration DLL staleness: build with `--no-incremental` when regenerating migrations.

## Notes for the crafter

- `WorkItemService.cs:103` (`WasBlocked`) and `:120-129` (`CollectDomainEvents`) are the shape to mirror.
- `FeatureStateTransition` + `SyncFeatureStateTransitions` (`:505`) show how the feature path already handles a per-feature history table — including that it saves in a second pass after `featureRepository.Save()`, because features have no id until saved.
- `feature.Id` is 0 until saved. Any capture keyed on it must run after the save, exactly as `SyncFeatureStateTransitions` does.
