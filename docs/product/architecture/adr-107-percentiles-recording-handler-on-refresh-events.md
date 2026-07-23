# ADR-107: Percentile/PBC snapshots recorded by domain-event handlers on the metrics-refresh events

- **Status**: Accepted
- **Date**: 2026-07-23
- **Feature**: epic-5427-percentiles-over-time (ADO Epic 5427)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

Epic 5427 must record the fresh percentile/PBC numbers **on each metrics refresh**, keeping exactly one row per `(owner, metric, horizon, calendar-day)` (DISCUSS D5; US-02 AC1–AC5). Two hard constraints from DISCUSS: recording must be **idempotent per day** under repeated same-day refresh, and a recording failure **must not break the refresh path** and must be observable (US-02 AC4 explicitly notes the Epic 5121 domain-event dispatcher *swallows* handler errors, so recording cannot rely on it surfacing them).

The precedent is exact. `BlockedCountSnapshotRecordingHandler` already implements `IDomainEventHandler<TeamDataRefreshed>` **and** `IDomainEventHandler<PortfolioFeaturesRefreshed>`; it reads today's metric off `ITeamMetricsService` / `IPortfolioMetricsService`, then upserts: `GetByPredicate(s => s.OwnerId == id && s.OwnerType == type && s.RecordedAt == today)`, update-in-place if found else `Add`, then `Save()`, with `today = DateOnly.FromDateTime(DateTime.Today)`. Both metrics services already expose the needed reads (`GetThroughputProcessBehaviourChart(...)` for PBC; percentile computations feed the existing point-in-time percentile widgets).

## Decision

Two new domain-event handlers on the **existing** refresh events, mirroring `BlockedCountSnapshotRecordingHandler`:

1. **`PercentilesOverTimeRecordingHandler`** : `IDomainEventHandler<TeamDataRefreshed>, IDomainEventHandler<PortfolioFeaturesRefreshed>` — computes CT percentiles for horizons 30/60/90 and WIA percentiles for today, upserts `PercentilesOverTimeSnapshot` rows.
2. **`ProcessBehaviorRecordingHandler`** : same two interfaces — computes UNPL/Average/LNPL per PBC metric type for today, upserts `ProcessBehaviorSnapshot` rows.

Both:
- **Idempotent per day** by upsert on the natural-key predicate (ADR-106), update-in-place on same-day re-run — exactly one row per key per day (US-02 AC2).
- **Forward-only** — no historical backfill of real data; the series starts the day recording begins (US-02 AC3).
- **Self-isolated failure**: each handler wraps its own work in `try/catch` and emits a structured recording-failed log/health signal on failure, rather than depending on the dispatcher to surface the error (US-02 AC4). The dispatcher's error-swallowing means the refresh path is *structurally* protected — a recording bug cannot break refresh — but silent, so the handler owns its own observability.

## Alternatives considered

- **Inline synchronous call at the end of the refresh service**: a recording bug then propagates into the refresh path and breaks it — a direct violation of US-02 AC4. **Rejected.**
- **Separate scheduled/background recorder**: duplicates the refresh trigger, drifts from the D5 "record on refresh" contract, and adds a scheduler with no independent-trigger justification. **Rejected.**
- **Extend `BlockedCountSnapshotRecordingHandler`** to also record percentiles/PBC: overloads one handler with three unrelated metric computations and two unrelated snapshot tables; the events are shared but the work is genuinely different. **Rejected** — same event subscription, separate handlers (single-responsibility).

## Consequences

- **Positive**: reuses the proven refresh-event seam and upsert idiom; idempotency and failure-isolation are inherited structurally, not re-engineered; adding a metric family later is a new handler on the same events.
- **Accepted cost**: the dispatcher swallows errors, so each handler must carry explicit try/catch + a failure log — verified by an AT asserting recording failure leaves the refresh green and emits the signal.
- **Reuse verdict**: refresh events `TeamDataRefreshed` / `PortfolioFeaturesRefreshed` → **EXTEND** (new subscribers); metrics services → **EXTEND** (read percentile/PBC as-of-today); handlers → **CREATE NEW** (new computation, same events — justified, not a reimplementation of blocked-count).
- Cross-refs [ADR-106](./adr-106-percentiles-over-time-snapshot-table-shape.md) (tables upserted), [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md) (domain-event bus + swallow semantics), [ADR-068](./adr-068-blocked-transition-capture-and-unblocked-event.md) / [ADR-069](./adr-069-blocked-count-snapshot-and-over-time-endpoint.md) (the snapshot-on-refresh pattern this mirrors).
