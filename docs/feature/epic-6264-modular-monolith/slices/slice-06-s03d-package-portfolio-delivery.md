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

- [ ] No file of this context remains in `Services/Interfaces` or `Services/Implementation`.
- [ ] Namespace rule green, existing pins preserved. Context DTOs moved after S12c.
- [ ] Moves separate from edits. ARCHITECTURE.md §4 updated.
- [ ] Behaviour-free, tests green, mutation score held.

## Dependencies

- Hard: S01, S02 gate.
- Soft: S05d, S07c, S12b.

## Notes for DESIGN

The update substrate has in-process and cluster adapters (Redis, Postgres lock). The composition root's adapter choice must keep working unchanged.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.
