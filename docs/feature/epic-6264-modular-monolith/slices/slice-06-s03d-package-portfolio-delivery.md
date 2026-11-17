# Slice 06 — S03d Package Portfolio / Delivery

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L**

## Goal

Move the Portfolio / Delivery context (updaters, update queue, delivery rules, delivery sources, write-back) into its own folder.

## IN

- `BackgroundServices/Update/**`, `Interfaces/Update`, `DeliverySources/**`, `DeliveryRuleService`, `WriteBack*`, `FeatureOrdering*`/`FeatureRank*` if S01 assigns them here.
- Delivery-owned `Models` (`Delivery*`, `DeliverySources/**`, `WriteBack/**`).
- Namespace ArchUnit rule. The existing pins (`DeliveryRuleServiceApiPreservationTest`, the `FeatureOrdering` comparer rule) are preserved.

## OUT

- Portfolio persistence ports (S05d). Deliveries, Portfolio and Portfolios controllers (S12b).

## Hypothesis

Portfolio/Delivery can be one context. **Disproved if** S01's map or the move shows it is really two (Portfolio vs Delivery). Then split this Story into two at DESIGN.

## Acceptance criteria

- [x] No file of this context remains in `Services/Interfaces` or `Services/Implementation`, for **Promises**, apart from 22 named exceptions with owners (the Delivery repositories for S05d, the write-back round for #6657, ordering and ranking for S03i), checked by `PromisesUnitTestPlacementTest`.
- [x] Namespace rule green, existing pins preserved (`DeliveryRuleServiceApiPreservationTest`, the `FeatureOrdering` comparer rule), plus a new order pin for `PortfolioForecastsUpdated`. Context-owned `Models` types that are not entities, wire types, port vocabulary or reached by the kernel have moved: the forecast block (2 types); the rest is listed for S04b and S12c. Context DTOs move after S12c.
- [ ] Moves separate from edits. ARCHITECTURE.md §4 updated.
- [ ] Behaviour-free, tests green, mutation score held.

## Dependencies

- Hard: S01, S02 gate.
- Soft: S05d, S07c, S12b.

## Notes for DESIGN

The update substrate has in-process and cluster adapters (Redis, Postgres lock). The composition root's adapter choice must keep working unchanged.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.

## Update after S01 (2026-11-01, ADR-270 accepted)

Retitled **Package Promises (Deliveries)**. ADR-270 splits today's Portfolio/Delivery module: Deliveries go to **Promises**; Portfolios (units) and their settings go to **Organisation set-up** (new Story S03i). The Delivery snapshot and closure handlers that react to "forecasts updated" sit in Promises and write Promises' own tables.

## Update after S03d (#6362, 2026-11-17)

Delivered in `Modules/Promises` (14 files: `Domain` 1, `Application` 11, `Application/Policies` 2; record:
`docs/evolution/2026-11-17-story-6362-s03d-package-promises-deliveries/evolution.md`). Size M, not L. The IN list is
corrected: `BackgroundServices/Update/**` and `Interfaces/Update` stay in the update code (platform lane, Epic 6474
slice 12), `WriteBack*` stays for #6657, and `FeatureOrdering*`/`FeatureRank*` go to Organisation set-up (S03i). The
Delivery-owned `Models` criterion follows S03a's four conditions, so only `DeliveryForecastBlock` moved. The composition
root's in-process and cluster update adapters are untouched, because nothing in the update code moved. Notes for later
Stories:

- **S05d** (#6372, Portfolio ports): the two Delivery repositories and `RecordableDeliveries` stay with the platform and wait for it.
- **S03i** (Organisation set-up): Feature ordering and ranking, `ManualRank`, and the Portfolio as a unit.
- **#6657**: places the write-back round (candidate: Forecasting `Application/WriteBack`).
- **Epic 6474 slice 12**: `PortfolioUpdater` still calls the Delivery rule and source sync services inline (the one listed entry of R-P4).
