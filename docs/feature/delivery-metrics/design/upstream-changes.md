# DESIGN → upstream changes (delivery-metrics)

Two changes touch DISCUSS artifacts. Change 1 is a consolidation; Change 2 is a product decision
(the user dropped the backfill). Neither drops a story, AC, or KPI — all DISCUSS series are still
delivered; US-01 is reframed to a forward-only burnup.

## Change 1 — Endpoint consolidation (DISCUSS driving-ports table → ONE endpoint)

**DISCUSS originally listed TWO endpoints** (`docs/feature/delivery-metrics/feature-delta.md` →
"Wave: DISCUSS / [REF] Driving ports"):

> | GET | `/api/portfolios/{portfolioId}/deliveries/{deliveryId}/metrics-history` | … | **New (Slice 1)** | … |
> | GET | `/api/portfolios/{portfolioId}/deliveries/{deliveryId}/metrics-history/forecast` | … | **New (Slice 3/4)** | … **May be folded into the history endpoint as additional series — a DESIGN choice.** |

**DESIGN decision (ADR-050, Decision 3)**: fold the `metrics-history/forecast` series INTO the single
`metrics-history` endpoint as additional (initially-null) series. DISCUSS explicitly delegated this
choice to DESIGN ("a DESIGN choice"), so this is an exercise of that delegation, not an override.

**Also adjusted**: the DISCUSS routes are written under `/api/portfolios/{portfolioId}/deliveries/{deliveryId}/…`.
DESIGN aligns the route to the existing `DeliveriesController` convention (`api/v1/[controller]` +
`api/latest/[controller]`, scope resolved from the delivery's portfolio for the `PortfolioRead`
guard): `GET /api/v1/deliveries/{deliveryId}/metrics-history`. Behaviour, auth scope, and RBAC are
unchanged; only the URL shape matches in-repo precedent.

**Impact**:
- US-03 / US-04 ACs that reference "the forecast endpoint" / "the trend endpoint" are satisfied by
  the forward series on the single endpoint — no AC text needs rewriting (they describe behaviour,
  not the route). DISTILL should phrase acceptance scenarios against the single endpoint.
- Cross-cutting Lighthouse-Clients: ONE version-gate registry entry instead of two; the Slice-1
  gate covers Slices 2-4's added series (no new gate at Slice 3/4 since no new endpoint). This is
  strictly simpler than the DISCUSS per-slice gating note and removes the "decide at Slice 3 whether
  it is a new sibling endpoint" branch.
- No story, KPI, or out-of-scope item changes.

## Change 2 — Backfill dropped: the store is forward-recorded only (user decision, 2026-06-02)

**Original DISCUSS assumption (D1 + D11, verbatim)**:

> D1: *"The slicing seam is reconstructable-by-backfill vs forward-only-recorded. Backlog+Done reconstruct retroactively from `WorkItem` dates + `FeatureStateTransition`."*
>
> D11: *"build ONE `DeliveryMetricSnapshot` store as the single time-series source of truth, fed two ways — (a) a one-time **backfill** that reconstructs actual-item backlog & done history into snapshot rows (full populated history on day one, NO separate live-query read path), and (b) a **forward recorder** … that appends what can't be reconstructed."*

**New assumption (forward-only, no backfill)**: the `DeliveryMetricSnapshot` store is fed by the
forward recorder ONLY. Every series — backlog, done, inferred estimate, forecast, likelihood/when-
distribution — accrues daily from the day recording begins. There is no retroactive reconstruction of
history from `WorkItem.CreatedDate`/`ClosedDate`. The chart starts empty at launch and fills one day
at a time, exactly like the forecast/likelihood trends. Re-opens are handled naturally because each
day snapshots the then-current count. The store stays unified — now because all series share the same
`(delivery, day)` grain and the same forward-recorded lifecycle.

**Rationale (user decision)**: accept delayed history (value accrues over weeks) in exchange for a
lighter Slice 1 (~1-1.5 days: store + migration + recorder hook + first chart, no backfill component),
no reconstruction-trustworthiness risk (re-opened items, bulk-import `CreatedDate` skew, mid-flight
feature re-scoping would all have made a reconstructed line diverge from team memory), and a uniformly
forward-only, honest model with no no-precedent backfill component.

**Impact**:
- US-01 reframed: the burnup is forward-only (no "full history on first open"); its AC describe the
  recorder writing the day's current counts and an idempotent same-day re-run, not a backfill pass.
- D6's forward-only empty state is now universal — ALL charts (the burnup too) show "builds forward
  from today — no snapshots recorded yet"; the journey's step-open-delivery-detail and
  step-read-backlog-done-history reflect that the burnup is forward-only.
- DESIGN Decisions 4 (backfill execution model) and 6 (backfill done-source / re-open cross-check) are
  removed as moot. ADR-048 is rewritten to a single forward-recorder feed
  (`adr-048-delivery-metric-snapshot-store.md`, superseding `…-dual-feed.md`).
- KPI timing caveat: KPIs 1-3 (chart consultation, spreadsheet-workaround reduction, scope-cut
  citations) can only be measured after enough forward history accrues per delivery.
- No story, KPI, or out-of-scope item is dropped; the 5-slice structure is unchanged (Slices 2-5 were
  already forward-fed).

## Change 3 — Chart placement refinement: tabs inside the delivery accordion (D2/D5, user choice 2026-06-02)

The charts live in a **"Metrics" tab inside the per-delivery `DeliverySection` accordion**, with the
existing feature grid in a "Work Items" tab; the Metrics tab is the lazy fetch trigger for
`metrics-history`. No new route (D2 intact). This refines the DESIGN Decision 5 wording ("inside the
per-delivery accordion") to the explicit tabs model across the brief, journey, feature-delta, and
ADRs where placement is mentioned.

No other DISCUSS artifact is changed by DESIGN.
