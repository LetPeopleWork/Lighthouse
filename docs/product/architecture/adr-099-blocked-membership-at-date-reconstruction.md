# ADR-099: Blocked membership at a past date is reconstructed from transition intervals, not persisted

- **Status**: ACCEPTED (2026-07-07)
- **Feature**: epic-5074-blocked-items (enhancement batch, slice 08 / B1)
- **Relates to**: ADR-068 (`WorkItemBlockedTransition` capture), ADR-069 (`BlockedCountSnapshot` over-time endpoint), ADR-072 (client version-gate matrix)

## Context

The Blocked-Items-over-time chart (ADR-069) plots a forward-only daily `BlockedCountSnapshot {OwnerId, OwnerType, RecordedAt, BlockedCount}` — a **count only**. The enhancement batch adds a drill-through: clicking a bar at date T should open a `WorkItemsDialog` of the items that were blocked at T. `BlockedCountSnapshot` does not record membership, so "which items at date T" is not directly available.

Three options were weighed:

- **(a) Current-only click** — only the latest bar is interactive (live `IsBlocked`); historical bars inert. Zero new backend. Rejected as the primary design (kept as the SPIKE fallback) — it does not answer the question the trend provokes ("which items drove that past spike?").
- **(b) Persist membership** — add an item-id set per snapshot. Amends ADR-069, needs an expand-only migration and backfill, and grows storage for a read that is infrequent. Rejected — cost out of proportion to an on-demand drill-through.
- **(c) Reconstruct on read from transition intervals** — derive membership at T from the `WorkItemBlockedTransition {WorkItemId, EnteredAt, LeftAt?}` intervals already captured (ADR-068). No new storage, no migration. **Chosen.**

## Decision

Items blocked at date T for an owner are **reconstructed on read** via an interval-overlap-at-point query over `WorkItemBlockedTransition`:

> a work item is blocked at T ⟺ it has a transition with `EnteredAt.Date ≤ T ∧ (LeftAt is null ∨ LeftAt.Date ≥ T)`,

joined to the owner's work items (`WorkItem.TeamId` for a Team; the feature owner for a Portfolio). The latest date reconstructs from live `IsBlocked` instead of the interval table.

Exposed as a new read endpoint mirroring `blockedCountHistory` (ADR-069):

`GET /api/{teams|portfolios}/{id}/metrics/blockedItemsAtDate?date=YYYY-MM-DD` → `WorkItemDto[]`

built with the same `GetEntityByIdAnExecuteAction` shape, and **version-gated** for the CLI/MCP clients per ADR-072 (new read endpoint). The frontend wires `BlockedItemsOverTimeChart`'s `BarChart onItemClick` to this endpoint and renders the result in the existing `WorkItemsDialog`.

`BlockedCountSnapshot` is **unchanged**; there is **no migration** and **no persisted membership**.

## Consequences

- **Positive**: no schema change, no migration, no storage growth; the drill-through reads a store that already exists; the single-source invariant (ADR-067/068 — one definition of "blocked", one capture) is preserved (reconstruction reads the same intervals that drive per-item duration).
- **Negative / bounded**: reconstruction is only valid back to when transition capture started (ADR-068 ship date). For a date before capture began, the dialog shows the partial set plus a "complete only from {captureStartDate}" note. L1 capture is sync-cadence resolution: a re-block within one cadence collapses to one interval — a documented ADR-068 limitation carried through here.
- **Reconciliation guard**: the reconstructed count for T is compared to `BlockedCountSnapshot.blockedCount` for T where both exist; a divergence surfaces a capture-gap note in the dialog rather than silently diverging. This doubles as the acceptance signal that reconstruction is faithful.
- **De-risk**: a ~2h pre-slice SPIKE (slice 08 brief) samples historical dates and checks reconstruction reconciles with the snapshot count within ±1 for ≥95% of samples; if not, fall back to option (a) current-only click.

## Alternatives considered

See options (a) and (b) above. (b) was the DISCUSS-time straw option; reconstruction (c) supersedes it because the drill-through is an on-demand read, not a hot path, so recomputing from intervals is cheaper end-to-end than persisting and migrating membership for every snapshot.
